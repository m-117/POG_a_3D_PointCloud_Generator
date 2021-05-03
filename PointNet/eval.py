# -*- coding: utf-8 -*-
"""
Created on Wed Apr 28 17:43:35 2021

@author: marco
"""

import numpy as np
import os
import csv
import tensorflow as tf
from keras import optimizers
from keras.layers import Input
from keras.models import Model, load_model
from keras.layers import Dense, Reshape
from keras.layers import Convolution1D, MaxPooling1D, BatchNormalization, Conv2D, MaxPooling2D
from keras.layers import Lambda, concatenate
from tensorflow.keras.initializers import Constant
from keras.utils import np_utils

num_points = 8192
# number of categories
k = 12
# epoch number
epo = 50
# batch size
batch_size = 32
# define optimizer
adam = optimizers.Adam(lr=0.001, decay=0.7)
# label colors
class2color = {0:	[0,255,0], #green ceiling
                 1:	[0,0,255], #blue floor
                 2:	[0,255,255], #turquoise wall
                 3: [255,255,0], #yellow light
                 4: [255,0,255], #purple bookshelf
                 5: [255,0,0], #red chair
                 6: [100, 100, 100], #grey table
                 7: [0, 75, 0], #darkgreen vase
                 8: [255,125,0], #orange lamp
                 9: [150,255,0], #lightgreen laptop
                 10: [0,150,255], #lightblue monitor
                 11: [0, 0, 0], #black sofa
                 12: [75,0,0], #darkred
} 


def sample_pc(data, indices):
    points = None
    labels = None
    
    for i in range(len(indices)):
        start = indices[i][0]
        end = indices[i][1]
        #if end - start < 1024:
        #    continue
        p, l = sample_pc_cluster(data[start:end, :])
        if points is None or labels is None:
            points = p
            labels = l
        else:
            points = np.vstack((points, p))
            labels = np.vstack((labels, l))
    point_array = points.reshape(-1, num_points, 9)
    label_array = labels.reshape(-1, num_points, 1)
    return (point_array, label_array)

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
    if cluster_data.shape[0] > num_points*2:
        index = np.random.choice(len(cluster_data), num_points*2, replace=False)
    else:
        index = np.random.choice(len(cluster_data), num_points*2, replace=True)
    for i in range(len(index)):
        selected_points.append(cluster_data[index[i]])
    selected_points = np.array(selected_points)
    data = np.take(selected_points, [0,1,2,3,4,5,6,7,8], axis=1)
    label = np.take(selected_points, [9], axis=1)
    return (data, label)

def write_processed_data(data, path, filename):
    
    with open(os.path.join(path, filename), 'w') as output:
        writer = csv.writer(output, delimiter=';', lineterminator='\n')
        for i in range(data.shape[0]):
            writer.writerow(np.around(data[i, :], 4))
            
def write_stats(iou, acc, path, filename):
    mean_iou = 0
    for i in range(k):
        mean_iou += iou[i]
    mean_iou = mean_iou / k
    
    with open(os.path.join(path, filename), 'w') as output:
        writer = csv.writer(output, delimiter=';', lineterminator='\n')
        writer.writerow("IoU per class:")
        writer.writerow(iou)
        writer.writerow("Mean IoU:")
        writer.writerow(mean_iou)
        writer.writerow("Accuracy:")
        writer.writerow(acc)
            
def change_color(data, labels):
    for i in range(data.shape[0]):
        col = int(labels[i])
        data[i, 3] = class2color[col][0] / 255
        data[i, 4] = class2color[col][1] / 255
        data[i, 5] = class2color[col][2] / 255
        
def evaluate(gt_labels, pred):
    
    print(gt_labels.shape)
    print(pred.shape)
    true_pos = [0 for _ in range(k)]
    false_pos = [0 for _ in range(k)]
    gt = [0 for _ in range(k)]
    
    for i in range(gt_labels.shape[0]):
        gt[int(gt_labels[i])] += 1
        if int(gt_labels[i]) == int(pred[i]):
            true_pos[int(gt_labels[i])] += 1
        else:
            false_pos[int(pred[i])] += 1
    iou_list = []
    acc = 0
    for j in range(k):
        if float(gt[j]) == 0:
            iou_list.append(-1)
            continue
        iou = true_pos[j]/float(gt[j] + false_pos[j])
        iou_list.append(iou)
        acc += true_pos[j]
    
    acc = acc / float(gt_labels.shape[0])
    
    return iou_list, acc


def main():
    path = os.path.dirname(os.path.realpath(__file__))
    model_path = os.path.join(path, "eval_models")
    data_path = os.path.join(path, "eval_data")
    pred_path = os.path.join(path, "predicted")
    filenames = [d for d in os.listdir(data_path)]
    models_for_eval = [x for x in os.listdir(model_path)]
    print(models_for_eval)
    
    for m in models_for_eval:
        p = os.path.join(model_path, m)
        model = load_model(p, compile=True)
        iou_list = [0 for _ in range(k)]
        mean_acc = 0
        output_path = os.path.join(pred_path, m)
        if not os.path.exists(output_path):    
            os.mkdir(output_path)
        for d in range(len(filenames)):
            progress = (d/len(filenames))*100
            print("Fileread started. Current progress: " + str(progress) +"%")
            cur_points, cluster_indices = load_csv(os.path.join(data_path, filenames[d]))
            eval_points, eval_labels = sample_pc(cur_points, cluster_indices)
            predictions = model.predict(eval_points)
            predictions = predictions.reshape(-1, k)
            predictions = predictions.argmax(1)
            eval_labels = eval_labels.reshape(-1, 1)
            iou, acc = evaluate(eval_labels, predictions)
            for i in range(k):
                if iou[i] >= 0:
                    iou_list[i] += iou[i]
            mean_acc += acc
            
            if d%10 == 0:
                eval_points = eval_points.reshape(-1, 9)
                output = np.empty_like(eval_points[:, :6])
                np.copyto(output, eval_points[:, :6])
                write_processed_data(output, output_path, "OG_" + m + "_" + filenames[d])
                change_color(output, eval_labels)
                write_processed_data(output, output_path, "GT_" + m + "_" + filenames[d])         
                change_color(output, predictions)
                write_processed_data(output, output_path, "Pred_" + m + "_" + filenames[d])
        for i in range(k):
            iou_list[i] = iou_list[i] / len(filenames)
        write_stats(iou_list, mean_acc/len(filenames), output_path, "Stats_Model_" + m + ".txt")


if __name__ == "__main__":
    main()