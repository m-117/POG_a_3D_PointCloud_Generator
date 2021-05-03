# -*- coding: utf-8 -*-

import numpy as np
import os
import logging
logging.getLogger('tensorflow').setLevel(logging.WARNING)
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
import tensorflow as tf
from keras import optimizers
from keras.layers import Input
from keras.models import Model, load_model
from keras.layers import Dense, Reshape
from keras.layers import Convolution1D, MaxPooling1D, BatchNormalization, Conv2D, MaxPooling2D
from keras.layers import Lambda, concatenate
from tensorflow.keras.initializers import Constant
from keras.utils import np_utils
#from keras.utils import np_utils
import h5py
from tensorflow.python.client import device_lib
print(device_lib.list_local_devices())


def mat_mul(A):
    return tf.matmul(A[0], A[1])


def exp_dim(global_feature, num_points):
    return tf.tile(global_feature, [1, num_points, 1])


def load_csv(csv_filename):
    file = np.genfromtxt(csv_filename, delimiter=';')
    cluster_indices = []
    start = 0
    cur = 0
    i = 0
    for i in range(file.shape[0]):
        if file[i, 0] == 1000:
            cur = i
            if cur != start:
                cluster_indices.append((start, cur))
                start = i+1
            elif cur == start:
                start = i+1
    
    return (file, cluster_indices)

def sample_pc_cluster(cluster_data):
    selected_points = []
    if cluster_data.shape[0] > num_points:
        index = np.random.choice(len(cluster_data), num_points, replace=False)
    else:
        index = np.random.choice(len(cluster_data), num_points, replace=True)
    for i in range(len(index)):
        selected_points.append(cluster_data[index[i]])
    selected_points = np.array(selected_points)
    data = np.take(selected_points, [0,1,2,3,4,5,6,7,8], axis=1)
    label = np.take(selected_points, [9], axis=1)
    return (data, label)


def choose_train_data(data, cluster_indices):
    train_points = []
    train_indices = []
    test_points = []
    test_indices = []
    index = np.random.choice(len(data), len(data), replace=False)
    end_train = int(len(index)*0.8)
    for i in range(end_train):
        train_points.append(data[index[i]])
        train_indices.append(cluster_indices[index[i]])
    for j in range(end_train, len(index)):
        test_points.append(data[index[j]])
        test_indices.append(cluster_indices[index[j]])
    return (train_points, train_indices, test_points, test_indices)

def sample_pc(data, indices):
    points = None
    labels = None
    points_accumulated = None
    labels_accumulated = None
    
    for i in range(len(data)):
        print("Start scene sample" + str(i))
        for j in range(len(indices[i])):
            start = indices[i][j][0]
            end = indices[i][j][1]
            if end - start < 512:
                continue
            p, l = sample_pc_cluster(data[i][start+1:end-1, :])
            if points is None or labels is None:
                points = p
                labels = l
            else:
                points = np.vstack((points, p))
                labels = np.vstack((labels, l))
        if i % 10 == 0 or i == len(data)-1:
            if points_accumulated is None or labels_accumulated is None:
                points_accumulated = points
                labels_accumulated = labels
                points = None
                labels = None
            else:
                points_accumulated = np.vstack((points_accumulated, points))
                labels_accumulated = np.vstack((labels_accumulated, labels))
                points = None
                labels = None
    point_array = points_accumulated.reshape(-1, num_points, 9)
    label_array = labels_accumulated.reshape(-1, num_points, 1)
    return (point_array, label_array)
        

'''
global variable
'''
# number of points in each sample
#262144
num_points = 8192  
# number of categories
k = 12
# epoch number
epo = 250
# batch size
b_size = 12
# adaptive flag
adaptive_training = False

LOG_DIR = os.path.join(os.path.dirname(os.path.realpath(__file__)), "logfiles")
CKPT_DIR = os.path.join(LOG_DIR, "ckpts")

# define optimizer
adam = optimizers.Adam(lr=0.001, decay=0.7)


'''
Pointnet Architecture
'''
# input_Transformation_net
input_points = Input(shape=(num_points, 9))
x = Convolution1D(64, 1, activation='relu',
                  input_shape=(num_points, 9))(input_points)
x = BatchNormalization()(x)
x = Convolution1D(128, 1, activation='relu')(x)
x = BatchNormalization()(x)
x = Convolution1D(1024, 1, activation='relu')(x)
x = BatchNormalization()(x)
x = MaxPooling1D(pool_size=num_points)(x)
x = Dense(512, activation='relu')(x)
x = BatchNormalization()(x)
x = Dense(256, activation='relu')(x)
x = BatchNormalization()(x)
x = Dense(9*9, kernel_initializer='zeros', bias_initializer=Constant(np.eye(9).flatten()), activity_regularizer=None)(x)
input_T = Reshape((9, 9))(x)

# forward net
g = Lambda(mat_mul)((input_points, input_T))
g = Convolution1D(64, 1, input_shape=(num_points, 9), activation='relu')(g)
g = BatchNormalization()(g)
g = Convolution1D(64, 1, input_shape=(num_points, 9), activation='relu')(g)
g = BatchNormalization()(g)

# feature transformation net
f = Convolution1D(64, 1, activation='relu')(g)
f = BatchNormalization()(f)
f = Convolution1D(128, 1, activation='relu')(f)
f = BatchNormalization()(f)
f = Convolution1D(1024, 1, activation='relu')(f)
f = BatchNormalization()(f)
f = MaxPooling1D(pool_size=num_points)(f)
f = Dense(512, activation='relu')(f)
f = BatchNormalization()(f)
f = Dense(256, activation='relu')(f)
f = BatchNormalization()(f)
f = Dense(64 * 64, weights=[np.zeros([256, 64 * 64]), np.eye(64).flatten().astype(np.float32)])(f)
feature_T = Reshape((64, 64))(f)

# forward net
g = Lambda(mat_mul)((g, feature_T))
seg_part1 = g
g = Convolution1D(64, 1, activation='relu')(g)
g = BatchNormalization()(g)
g = Convolution1D(128, 1, activation='relu')(g)
g = BatchNormalization()(g)
g = Convolution1D(1024, 1, activation='relu')(g)
g = BatchNormalization()(g)

# global_feature
global_feature = MaxPooling1D(pool_size=num_points)(g)
global_feature = Lambda(exp_dim, arguments={'num_points': num_points})(global_feature)

# point_net_seg
c = concatenate([seg_part1, global_feature])
c = Convolution1D(512, 1, activation='relu')(c)
c = BatchNormalization()(c)
c = Convolution1D(256, 1, activation='relu')(c)
c = BatchNormalization()(c)
c = Convolution1D(128, 1, activation='relu')(c)
c = BatchNormalization()(c)
c = Convolution1D(128, 1, activation='relu')(c)
c = BatchNormalization()(c)
prediction = Convolution1D(k, 1, activation='softmax')(c)
'''
end of pointnet
'''

# define model
model = Model(inputs=input_points, outputs=prediction)
print(model.summary())

'''
load train and test data
'''

# load TRAIN points and labels
path = os.path.dirname(os.path.realpath(__file__))
data_path = os.path.join(path, "processed_data")
filenames = [d for d in os.listdir(data_path)]
last_trainindex = int(len(filenames)*0.8)
all_points = []
all_indices = []
train_points = []
train_indices = []
train_labels = []
test_points = []
test_indices = []
test_labels = []
train_points_t = []
test_points_t = []

for i in range(10):
    all_points.append([])
    all_indices.append([])
    train_points.append([])
    train_indices.append([])
    train_labels.append([])
    test_points.append([])
    test_indices.append([])
    test_labels.append([])
    train_points_t.append([])
    test_points_t.append([])


if adaptive_training:
    for d in range(len(filenames)):
        progress = (d/len(filenames))*100
        print(filenames[d])
        print("Fileread started. Current progress: " + str(progress) +"%")
        cur_points, cluster_indices = load_csv(os.path.join(data_path, filenames[d]))
        diff = int(filenames[d].split("_")[2])
        print("Order into list", diff)
        all_points[diff-1].append(cur_points)
        all_indices[diff-1].append(cluster_indices)
else:
    for d in range(len(filenames)):
        progress = (d/len(filenames))*100
        print(filenames[d])
        print("Fileread started. Current progress: " + str(progress) +"%")
        cur_points, cluster_indices = load_csv(os.path.join(data_path, filenames[d]))
        print("Order into list", d%10)
        all_points[d%10].append(cur_points)
        all_indices[d%10].append(cluster_indices)

for j in range(10):
    train_points[j], train_indices[j], test_points[j], test_indices[j] = choose_train_data(all_points[j], all_indices[j]) 


'''
train and evaluate the model
'''
# compile classification model
model.compile(optimizer='adam',
              loss='categorical_crossentropy',
              metrics=['accuracy'])

score = None
old_score = None
flag = False

for i in range(int(epo/5)):

    if adaptive_training:
        if score == None or old_score == None:
            index = [0, 1]
        else:
            delta_acc = score[1] - old_score[1]
            if delta_acc > 0:
                if abs(delta_acc) >= 0.1:
                    index[0] += 2
                    index[1] += 2
                    flag = False
                elif abs(delta_acc) >= 0.03:
                    index[0] += 1
                    index[1] += 1
                    flag = False
                elif abs(delta_acc) < 0.03:
                    if flag:
                        index[0] += 1
                        index[1] += 1
                        flag = False
                    else:
                        flag = True
            else:
                if abs(delta_acc) >= 0.1:
                    index[0] -= 2
                    index[1] -= 2
                    flag = False
                elif abs(delta_acc) >= 0.03:
                    index[0] -= 1
                    index[1] -= 1
                    flag = False
                elif abs(delta_acc) < 0.03:
                    if flag:
                        index[0] += 1
                        index[1] += 1
                        flag = False
                    else:
                        flag = True
            index[0] = index[0] if index[0] < 9 else 8
            index[1] = index[1] if index[1] < 10 else 9
            index[0] = index[0] if index[0] >= 0 else 0
            index[1] = index[1] if index[1] >= 1 else 1    
    else:
        index = [i%10, (i+1)%10]
    
    print(index)
    for j in index:
        print(j)
        train_points_t[j], train_labels[j] = sample_pc(train_points[j], train_indices[j])
        test_points_t[j], test_labels[j] = sample_pc(test_points[j], test_indices[j])
        train_labels[j] = np_utils.to_categorical(train_labels[j], k)
        test_labels[j] = np_utils.to_categorical(test_labels[j], k)
    
    train_data_b = np.vstack((train_points_t[index[0]], train_points_t[index[1]]))
    train_labels_b = np.vstack((train_labels[index[0]], train_labels[index[1]]))
    test_data_b = np.vstack((test_points_t[index[0]], test_points_t[index[1]]))
    test_labels_b = np.vstack((test_labels[index[0]], test_labels[index[1]]))
   

    print("start epoch")
    model.fit(train_data_b, train_labels_b, batch_size=b_size, epochs=5, shuffle=True, verbose=1)
    old_score = score
    score = model.evaluate(test_data_b, test_labels_b, batch_size=b_size, verbose=1)
    print('Test loss: ', score[0])
    print('Test accuracy: ', score[1])
    model.save("trained_model_epoch_" + str((i+1)*5) + "_testloss_" + str(score[0]) + "_testacc_" + str(score[1]) + ".pb")
  
