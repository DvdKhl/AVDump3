using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Misc {
    public static class Clustering {
        public static double[][] KMeans<T>(int k, T[] data, Func<T, double> getKey, Func<T, int> getValue) { // TODO: Rewrite
            double[] center = new double[k];
            int[] clusterMap = new int[data.Length];


            int minIndex = 0, maxIndex = 0;
            center[0] = getKey(data[data.Length / (k + 1)]);
            double distance, maxDistance, minDistance;
            for(int i = 1; i < k; i++) {
                maxDistance = 0;

                for(int j = 0; j < data.Length; j++) {
                    minDistance = -1;
                    for(int l = 0; l < i; l++) {
                        distance = Math.Abs(center[l] - getKey(data[j]));
                        if(minDistance == -1 || distance < minDistance) {
                            minDistance = distance;
                            minIndex = j;
                        }
                    }
                    if(minDistance > maxDistance) {
                        maxDistance = minDistance;
                        maxIndex = minIndex;
                    }
                }
                center[i] = getKey(data[maxIndex]);
            }

            bool hasChanged;
            do {
                hasChanged = false;
                for(int i = 0; i < data.Length; i++) {
                    minDistance = -1;
                    for(int j = 0; j < k; j++) {
                        distance = Math.Abs(center[j] - getKey(data[i]));
                        if(minDistance == -1 || distance < minDistance) {
                            minDistance = distance;
                            minIndex = j;
                        }
                    }
                    if(clusterMap[i] != minIndex) {
                        clusterMap[i] = minIndex;
                        hasChanged = true;
                    }
                }

                int count;
                double sum;
                for(int i = 0; i < k; i++) {
                    sum = count = 0;
                    for(int j = 0; j < clusterMap.Length; j++) {
                        if(clusterMap[j] == i) {
                            sum += getKey(data[j]) * getValue(data[j]);
                            count += getValue(data[j]);
                        }
                    }
                    center[i] = sum / count;
                }


            } while(hasChanged);

            double[][] bla = new double[data.Length][];
            for(int i = 0; i < data.Length; i++) {
                bla[i] = new double[] { clusterMap[i], getKey(data[i]), getValue(data[i]) };
            }


            var q = from entry in bla orderby center[(int)entry[0]], Math.Abs(entry[1] - center[(int)entry[0]]) select entry;

            var q2 = q.GroupBy(c => c[0]).Select(a => new double[] { center[(int)a.Key], a.Sum(b => b[2]) });

            return q2.ToArray();
        }
    }
}
