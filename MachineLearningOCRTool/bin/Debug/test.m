%Initialization
clear ; close all ; clc

%%Setup the parameters
input_layer_size = 400;    %20x20 Input Images of Digits
num_labels = 48;           %10 labels, from 1 to 10
                           
data=load('C:\Users\Burds\Desktop\LASTNEW\Project6\MachineLearningOCRTool\bin\Debug\Jap11.txt'); %training data stored in arrays X,Y
X= data(:, 1:end-1);
Y = data(:, end);
m = size(X, 1);
                           
%Randomly select 100 data points to display
rand_indices = randperm(m);
sel = X(rand_indices(1:100), :);
displayData(sel);

fprintf('\nTraining One-vsAll Logistic Regression...\n')

lambda = 0.5;
[all_theta] = oneVsAll(X, Y, num_labels, lambda);

pred = predictOneVsAll(all_theta, X);
fprintf('\nTraining Set Accuracy: %f\n', mean(double(pred == Y)) * 100);
fprintf('Exporting model...\n');
save -ascii C:\Users\Burds\Desktop\LASTNEW\Project6\MachineLearningOCRTool\bin\Debug\LearnedJap190.txt all_theta;