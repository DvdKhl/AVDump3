using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Misc {
    public static class Clustering {
        public static double[][] KMeans(int k, KeyValuePair<double, int>[] data) { // TODO: Rewrite
            double[] center = new double[k];
            int[] clusterMap = new int[data.Length];


            int minIndex = 0, maxIndex = 0;
            center[0] = data[data.Length / (k + 1)].Key;
            double distance, maxDistance, minDistance;
            for(int i = 1; i < k; i++) {
                maxDistance = 0;

                for(int j = 0; j < data.Length; j++) {
                    minDistance = -1;
                    for(int l = 0; l < i; l++) {
                        distance = Math.Abs(center[l] - data[j].Key);
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
                center[i] = data[maxIndex].Key;
            }

            bool hasChanged;
            do {
                hasChanged = false;
                for(int i = 0; i < data.Length; i++) {
                    minDistance = -1;
                    for(int j = 0; j < k; j++) {
                        distance = Math.Abs(center[j] - data[i].Key);
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
                            sum += data[j].Key * data[j].Value;
                            count += data[j].Value;
                        }
                    }
                    center[i] = sum / count;
                }


            } while(hasChanged);

            double[][] bla = new double[data.Length][];
            for(int i = 0; i < data.Length; i++) {
                bla[i] = new double[] { clusterMap[i], data[i].Key, data[i].Value };
            }


            var q = from entry in bla orderby center[(int)entry[0]], Math.Abs(entry[1] - center[(int)entry[0]]) select entry;

            var q2 = q.GroupBy(c => c[0]).Select(a => new double[] { center[(int)a.Key], a.Sum(b => b[2]) });

            return q2.ToArray();
        }
    }
}
