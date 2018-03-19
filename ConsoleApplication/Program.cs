using System;
using System.IO;

namespace ConsoleApplication {
    class Program {
        public static String filename = "SampleData.txt"; 

        public static int SIZE_OF_POPULATION = 10000;
        public static int NUMBER_OF_GENERATIONS = 1000;
        public static int randomSeed = 1;
        public static int repairAndImproveEachNumberOfGeneration = 10;
        public static int swapToImproveEachNumberOfGeneration = 20;

        public static double mutationRate; // will be 1/L

        // It will be same for each instance;
        public static int numberOfItems;
        public static int capacity;
        public static int[,] items;

        // update these every time;
        public static bool[,] GeneticAlgorithm;
        public static int[] totalValues;
        public static int[] totalWeights;

        public static Random rand = new Random(randomSeed);

        public static void Main(String[] args) {
            if (args.Length > 0)
                ReadData(args[0]);
            else
                ReadData("");

            mutationRate = 1.0 / numberOfItems;
            adjustSizeAndGeneration();
            
            InitializeIndividuals(); // GeneticAlgorithm
            int bestFitness = 0;
            bool[] bestSolution = new bool[numberOfItems];

            for (int i = 0; i < NUMBER_OF_GENERATIONS; i++) { // it will use binary tournament selection                
                findTotalWeightAndValues(); // for GeneticAlgorithm(pre-Gen)
                if (i %  repairAndImproveEachNumberOfGeneration == 0)
                    repairAndImproveIndividuals(); // for GeneticAlgorithm(pre-Gen), updates totalweights and totalvalues
                if (i % swapToImproveEachNumberOfGeneration == 0 )
                    swapALLItemsToImproveSomeIndividuals(); // for GeneticAlgorithm(pre-Gen), updates totalweights and totalvalues

                bool[,] copy = (bool[,])GeneticAlgorithm.Clone(); // it was before findTotalWeightAndValues();

                binaryTournamentSelection(copy); // select from GeneticAlgorithm(previos Generation) into copy (new generation)
                UniformCrossover(copy); // 1. and 2. , 3. and 4.,...
                mutation(copy);

                int current = findBestSolutionFromPreviousGen(); // Uses totalValues;totalWeights;

                if (current != -1) {
                    if (bestFitness < totalValues[current]) {
                        bestFitness = totalValues[current];
                        bestSolution = extract(current);
                    }
                }

                GeneticAlgorithm = (bool[,])copy.Clone();
            }

            Console.WriteLine(bestFitness); // Console.WriteLine("Best Result: " + bestFitness);
            for (int j = 0; j < numberOfItems; j++) {
                if(bestSolution[j])
                    Console.Write("1 "); // Console.Write("1("+ items[j,0]+" weight: "+ items[j, 1] + ") ");
                else
                    Console.Write("0 ");
            }
            Console.ReadLine();
        }
        private static void adjustSizeAndGeneration() {
            if (numberOfItems > 1000) {
                SIZE_OF_POPULATION = 15000; // 60000 - 40   400000-10
                NUMBER_OF_GENERATIONS = 22;
                GeneticAlgorithm = new bool[SIZE_OF_POPULATION, numberOfItems];
            } else if (numberOfItems > 100) {
                SIZE_OF_POPULATION = 25000; // 60000 - 40   400000-10
                NUMBER_OF_GENERATIONS = 22;
                GeneticAlgorithm = new bool[SIZE_OF_POPULATION, numberOfItems];
            }
        }
        private static void swapALLItemsToImproveSomeIndividuals() { // it is guaranteed that all solutions are feasible.
            for (int index = 0; index < 20; index++) { // single swap for all solutions
                int solutionIndex = rand.Next(SIZE_OF_POPULATION);
                while (true) {
                    int itemIn = -1;
                    int itemOut = -1;
                    int maxGain = 0;

                    for (int indexItemOut = 0; indexItemOut < numberOfItems; indexItemOut++) {
                        for (int indexItemIn = 0; indexItemIn < numberOfItems; indexItemIn++) {
                            if (GeneticAlgorithm[solutionIndex, indexItemOut] && !GeneticAlgorithm[solutionIndex, indexItemIn]) { // if 1st item is already inside and other is not.
                                if (totalWeights[solutionIndex] - items[indexItemOut, 1] + items[indexItemIn, 1] <= capacity) { // if it is still feasible
                                    int gain = items[indexItemIn, 0] - items[indexItemOut, 1];
                                    if (gain > maxGain) {
                                        itemIn = indexItemIn;
                                        itemOut = indexItemOut;
                                        maxGain = gain;
                                    }
                                }
                            }
                        }
                    }
                    if (maxGain < 0) { // swap
                        GeneticAlgorithm[solutionIndex, itemOut] = false;
                        GeneticAlgorithm[solutionIndex, itemIn] = true;
                        totalWeights[solutionIndex] = totalWeights[solutionIndex] - items[itemOut, 1] + items[itemIn, 1];
                    } else {
                        break;
                    }
                }
            }
        }
        private static void repairAndImproveIndividuals() { // works on GeneticAlgorithm[,]
            for (int index = 0; index < SIZE_OF_POPULATION; index++) {
                if (totalWeights[index] > capacity) {
                    repairSolution(index);
                } else {
                    improveSolution(index);
                }
            }
        }
        private static void improveSolution(int solutionIndex) {
            bool notFinished = true;
            while (notFinished) {
                double bestRatio = 0;
                int bestRatioItemIndex = -1;
                for (int j = 0; j < numberOfItems; j++) {
                    if (!GeneticAlgorithm[solutionIndex, j] && (totalWeights[solutionIndex] + items[j, 1] <= capacity)) {
                        if (bestRatio < ((double)items[j, 0] / (double)items[j, 1])) {
                            bestRatio = ((double)items[j, 0] / (double)items[j, 1]);
                            bestRatioItemIndex = j;
                        }
                    }
                }
                if (bestRatioItemIndex != -1) {
                    GeneticAlgorithm[solutionIndex, bestRatioItemIndex] = true;
                    totalWeights[solutionIndex] = totalWeights[solutionIndex] + items[bestRatioItemIndex, 1];
                    totalValues[solutionIndex] = totalValues[solutionIndex] + items[bestRatioItemIndex, 0];
                } else { 
                    notFinished = false;
                }
            }
        }
        private static void repairSolution(int solutionIndex) {
            bool notRepaired = true;
            while (notRepaired) {
                double worstRatio = Double.MaxValue; // min value/weight
                int worstRatioItemIndex = -1;
                for (int j = 0; j < numberOfItems; j++) {
                    if (GeneticAlgorithm[solutionIndex, j]) {
                        if (worstRatio > ((double)items[j, 0] / (double)items[j, 1])) {
                            worstRatio = ((double)items[j, 0] / (double)items[j, 1]);
                            worstRatioItemIndex = j;
                        }
                    }
                }
                if (worstRatioItemIndex != -1) {
                    GeneticAlgorithm[solutionIndex, worstRatioItemIndex] = false;
                    totalWeights[solutionIndex] = totalWeights[solutionIndex] - items[worstRatioItemIndex, 1];
                    totalValues[solutionIndex] = totalValues[solutionIndex] - items[worstRatioItemIndex, 0];
                    if (totalWeights[solutionIndex] <= capacity) {
                        notRepaired = false;
                    }
                } else { // theoratically impossible to get here.
                    notRepaired = false; 
                }
            }
        }
        private static void UniformCrossover(bool[,] copy) {
            for (int i = 0; i < SIZE_OF_POPULATION; i=i+2) {
                for (int j = 0; j < numberOfItems; j++) {
                    if (rand.NextDouble() < 0.5) { // swapping
                        bool temp = copy[i, j];
                        copy[i, j] = copy[i + 1, j];
                        copy[i + 1, j] = temp;
                    }
                }
            }
        }
        private static void mutation(bool[,] copy) {
            for (int i = 0; i < SIZE_OF_POPULATION; i++) {
                for (int j = 0; j < numberOfItems; j++) {
                    if (rand.NextDouble() < mutationRate) { // swapping
                        copy[i, j] = !copy[i, j];
                    }
                }
            }
        }
        private static void binaryTournamentSelection(bool[,] copy) {
            for (int i = 0; i < SIZE_OF_POPULATION; i++) {
                int index1 = rand.Next(SIZE_OF_POPULATION);
                int index2 = rand.Next(SIZE_OF_POPULATION);
                if (index1 == index2) {
                    if (index2 == SIZE_OF_POPULATION-1) {
                        index2 = 0;
                    } else {
                        index2++;
                    }
                }

                int index = comparePreGen(index1,index2); // return index1 or index2
                for (int j = 0; j < numberOfItems; j++)
                    copy[i, j] = GeneticAlgorithm[index, j];
            }
        }
        private static int comparePreGen(int index1, int index2) {
            bool exceed1 = totalWeights[index1] > capacity;
            bool exceed2 = totalWeights[index2] > capacity;

            if (!exceed1 && !exceed2) {
                if (totalValues[index1] > totalValues[index2])
                    return index1;
                else
                    return index2;
            } else if (!exceed1 && exceed2) {
                return index1;
            } else if (exceed1 && !exceed2) {
                return index2;
            } else { // both exceeding
                if (((double)totalValues[index1] / (double)totalWeights[index1]) > ((double)totalValues[index2] / (double)totalWeights[index2]))  // if ((totalValues[index1] / totalWeights[index1]) > (totalValues[index2] / totalWeights[index2])) (1->20) // if (((double)totalValues[index1] / (double)totalWeights[index1]) > ((double)totalValues[index2] / (double)totalWeights[index2])) 
                    return index1;
                else
                    return index2;
            }
        }
        private static int findBestSolutionFromPreviousGen() {
            int best = -1;
            int bestIndex = -1;
            for (int i = 0; i < SIZE_OF_POPULATION; i++) { // from GeneticAlgorithm
                if (totalWeights[i] <= capacity) {
                    if (totalValues[i] > best) {
                        best = totalValues[i];
                        bestIndex = i;
                    }
                }
            }
            return bestIndex;
        }
        private static bool[] extract(int index) {
            bool[] bestSolution = new bool[numberOfItems];
            for (int i = 0; i < numberOfItems;i++) {
                bestSolution[i] = GeneticAlgorithm[index, i]; // boolean 1,0,0,1
            }
            return bestSolution;
        }
        private static void findTotalWeightAndValues() {
            totalValues = new int[SIZE_OF_POPULATION];
            totalWeights = new int[SIZE_OF_POPULATION];

            for (int i = 0; i < SIZE_OF_POPULATION; i++) {
                int totalWeight = 0;
                int totalValue = 0;
                for (int j = 0; j < numberOfItems; j++) {
                    if (GeneticAlgorithm[i, j]) {
                        totalValue = totalValue + items[j, 0];
                        totalWeight = totalWeight + items[j, 1];
                    }
                }
                totalValues[i] = totalValue;
                totalWeights[i] = totalWeight;
            }
        }
        private static void InitializeIndividuals() {  // change 0.5 with capacity / totalWeight of all items
            double totalWeightOfAllItems = 0;
            for (int i = 0; i < numberOfItems; i++) {
                totalWeightOfAllItems = totalWeightOfAllItems + items[i,1];
            }
            double probability = capacity / totalWeightOfAllItems;

            for (int i = 0; i < SIZE_OF_POPULATION; i++) {
                for (int j = 0; j < numberOfItems; j++) {
                    GeneticAlgorithm[i, j] = rand.NextDouble() < probability ? true : false;
                }
            }
        }
        private static void ReadData(string filenamex) {
            try {
                String file_name = "";
                if (filenamex.Equals(""))
                    file_name = filename;
                else
                    file_name = filenamex;

                String[] texts;
                using (StreamReader sr = new StreamReader(file_name)) {
                    String fullText = sr.ReadToEnd();
                    texts = fullText.Split('\n');
                }
                String[] firstLineData = System.Text.RegularExpressions.Regex.Split(texts[0], @"\s+");
                numberOfItems = Int32.Parse(firstLineData[0]);
                capacity = Int32.Parse(firstLineData[1]);
                GeneticAlgorithm = new bool[SIZE_OF_POPULATION, numberOfItems];
                items = new int[numberOfItems, 2]; // value and weight


                for (int i = 1; i < numberOfItems + 1; i++) {
                    String[] LineData = System.Text.RegularExpressions.Regex.Split(texts[i], @"\s+");
                    int value = Int32.Parse(LineData[0]);
                    int weight = Int32.Parse(LineData[1]);

                    items[i - 1, 0] = value;
                    items[i - 1, 1] = weight;
                }
            } catch (Exception e) {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }
    }
}
