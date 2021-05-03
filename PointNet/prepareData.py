# -*- coding: utf-8 -*-

import numpy as np
import csv
import os

num_points = 4096
cluster_end_row = [1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000]
cluster_indices = np.zeros((5, 5))
for i in range(5):
    for j in range(5):
        cluster_indices[i, j] =  i + j*5

# load pc from csv file
def load_csv(csv_filename):
    f = np.genfromtxt(csv_filename, delimiter=';')
    f = np.delete(f, (0), axis=0)
    data = np.take(f, [0,1,2,3,4,5], axis=1)
    label = np.take(f, [6], axis=1)
    return (data, label)

def add_normalized_coords(data):
    new_coords = np.empty_like(data[:, :3])
    np.copyto(new_coords, data[:, :3])
    new_coords += (np.max(new_coords[:, :3], axis=0) - np.min(new_coords[:, :3], axis=0))/2
    new_coords /= np.max(new_coords[:, :3], axis=0) - np.min(new_coords[:, :3], axis=0)
    return np.hstack((data, new_coords))
    
def order_points_25(data):
    ordered_points = []
    for i in range(25):
        ordered_points.append([])
    for i in range(data.shape[0]):
        x, z = int((data[i, 6]/0.2)), int((data[i, 8]/0.2))
        x = x if x<5 else 4
        z = z if z<5 else 4
        index = int(cluster_indices[x, z])
        ordered_points[index].append(data[i])
    for i in range(len(ordered_points)):
        ordered_points[i] = np.array(ordered_points[i])
    return ordered_points

def order_points_8(data):
    ordered_points = [[],[],[],[],[],[],[],[]]
    for i in range(data.shape[0]):
        x, y, z = data[i, 6:9]
        if x<0.5 and z<0.5:
            index = 0 if y<0.5 else 4
        elif x>=0.5 and z<0.5:
            index = 1 if y<0.5 else 5
        elif x<0.5 and z>=0.5:
            index = 2 if y<0.5 else 6
        elif x>=0.5 and z>=0.5:
            index = 3 if y<0.5 else 7
        ordered_points[index].append(data[i])
    for i in range(len(ordered_points)):
        ordered_points[i] = np.array(ordered_points[i])
    return ordered_points

def order_points_4(data):
    ordered_points = [[],[],[],[]]
    for i in range(data.shape[0]):
        x, z = data[i, 6], data[i, 8]
        if x<0.5 and z<0.5:
            index = 0
        elif x>=0.5 and z<0.5:
            index = 1
        elif x<0.5 and z>=0.5:
            index = 2
        elif x>=0.5 and z>=0.5:
            index = 3
        ordered_points[index].append(data[i])
    for i in range(len(ordered_points)):
        ordered_points[i] = np.array(ordered_points[i])
    return ordered_points

    

def write_processed_data(data, path, filename):
    with open(os.path.join(path, filename), 'w') as output:
        writer = csv.writer(output, delimiter=';', lineterminator='\n')
        for item in data:
            writer.writerows(np.around(item, 4))
            writer.writerow(cluster_end_row)

def main():
    path = os.path.dirname(os.path.realpath(__file__))
    raw_data_path = os.path.join(path, "data")
    prep_data_path = os.path.join(path, "processed_data")
    filenames = [d for d in os.listdir(raw_data_path)]
    for i in range(len(filenames)):    
        cur_points, cur_labels = load_csv(os.path.join(raw_data_path, filenames[i]))
        cur_points = add_normalized_coords(cur_points)
        print(max(cur_labels))
        print(min(cur_labels))
        ordered_points = order_points_25(np.hstack((cur_points, cur_labels)))
        write_processed_data(ordered_points, prep_data_path, filenames[i])

if __name__ == "__main__":
    main()

