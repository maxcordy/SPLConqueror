﻿using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;


namespace MachineLearning.Sampling.Hybrid.Distributive.SelectionHeuristic
{
    /// <summary>
    /// This class is another selection mechanism by using the Microsoft Solver Foundation solver.
    /// The advantage of this class is, that the whole population is not needed, since it relies
    /// only on the solver.
    /// Be aware that this selection method currently works only for binary features.
    /// </summary>
    public class SolverSelection : ISelectionHeuristic
    {
        // The seed for the random-class
        private int seed = 0;

        private Solver.VariantGenerator generator = new Solver.VariantGenerator();

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="MachineLearning.Sampling.Hybrid.Distributive.SelectionHeuristic.SolverSelection"/> class.
        /// </summary>
        public SolverSelection ()
        {
        }

        /// <summary>
        /// Sets the seed of the random number generator.
        /// </summary>
        /// <param name="seed">Value of the seed.</param>
        public void setSeed(int seed)
        {
            this.seed = seed;
        }

        /// <summary>
        /// This method selects a given number of configurations by using the sample mechanism of the CSP solver.
        /// </summary>
        /// <returns>A set of configurations that have been picked to fit the given distribution.</returns>
        /// <param name="wantedDistribution">The wanted distribution of the samples.</param>
        /// <param name="allBuckets">The buckets containing at least one configuration.</param>
        /// <param name="count">The number of configurations to select.</param>
        public List<Configuration> SampleFromDistribution(Dictionary<double, double> wantedDistribution, List<double> allBuckets, int count) {
            Random rand = new Random(seed);
            List<Configuration> selectedConfigurations = new List<Configuration>();
            Dictionary<int, Configuration> selectedConfigurationsFromBucket = new Dictionary<int, Configuration>();
            for (int i = 0; i < allBuckets.Count; i++)
            {
                selectedConfigurationsFromBucket[i] = null;
            }

            // Create and initialize the weight function
            Dictionary<BinaryOption, int> featureWeight = InitializeWeightDict(GlobalState.varModel);

            bool[] noSamples = new bool[allBuckets.Count];


            while (selectedConfigurations.Count < count && HasSamples(noSamples)) {
                double randomDouble = rand.NextDouble();
                double currentProbability = 0;
                int currentBucket = 0;

                while (randomDouble > currentProbability + wantedDistribution.ElementAt(currentBucket).Value)
                {
                    currentBucket++;
                    currentProbability += wantedDistribution.ElementAt(currentBucket).Value;
                }

                // Note: This method works only for binary features and therefore, only integer buckets
                int distanceOfBucket = Convert.ToInt32(wantedDistribution.ElementAt(currentBucket).Key);

                // Repeat if there are currently no solutions in the bucket.
                // This is intended to reduce the work of the solver.
                if (noSamples [currentBucket]) {
                    continue;
                }

                // Now select the configuration by using the solver
                List<BinaryOption> solution = generator.WeightMinimization(GlobalState.varModel, distanceOfBucket, featureWeight, selectedConfigurationsFromBucket[currentBucket]);

                // If a bucket was selected that now contains no more configurations, repeat the procedure
                if (solution == null)
                {
                    noSamples [currentBucket] = true;
                    continue;
                }

                Configuration currentSelectedConfiguration = new Configuration (solution);

                selectedConfigurations.Add (currentSelectedConfiguration);
                selectedConfigurationsFromBucket[currentBucket] = currentSelectedConfiguration;
                UpdateWeights(GlobalState.varModel, featureWeight, currentSelectedConfiguration);
            }

            if (selectedConfigurations.Count < count)
            {
                GlobalState.logError.logLine("Sampled only " + selectedConfigurations.Count + " configurations as there are no more configurations.");
            }
            return selectedConfigurations;
        }

        /// <summary>
        /// This method initializes the weight function. 
        /// </summary>
        /// <returns>A new dictionary with initialized values.</returns>
        /// <param name="vm">The variability model.</param>
        private Dictionary<BinaryOption, int> InitializeWeightDict(VariabilityModel vm)
        {
            List<BinaryOption> features = vm.BinaryOptions;
            Dictionary<BinaryOption, int> weights = new Dictionary<BinaryOption, int>();

            foreach (BinaryOption binOpt in features)
            {
                weights.Add(binOpt, 0);
            }

            return weights;
        }

        /// <summary>
        /// This method updates the weight-dictionary by incrementing the counts.
        /// </summary>
        /// <param name="vm">The variability model.</param>
        /// <param name="weights">The dictionary containing the number of features in all configurations.</param>
        /// <param name="addedConfiguration">The newly added configuration.</param>
        private void UpdateWeights(VariabilityModel vm, Dictionary<BinaryOption, int> weights, Configuration addedConfiguration) {
            List<BinaryOption> features = vm.BinaryOptions;

            foreach (BinaryOption binOpt in features) {
                if (addedConfiguration.BinaryOptions.ContainsKey(binOpt) && addedConfiguration.BinaryOptions[binOpt] == BinaryOption.BinaryValue.Selected) {
                    weights [binOpt]++;
                }
            }
        }

        /// <summary>
        /// Returns whether there are more samples or not.
        /// </summary>
        /// <returns><c>true</c> if there is at least one sample; otherwise, <c>false</c>.</returns>
        /// <param name="noSamples">The array containing the information if a bucket has no more samples.</param>
        private bool HasSamples(bool[] noSamples) {
            foreach (bool b in noSamples) {
                if (!b) {
                    return true;
                }
            }
            return false;
        }
            
    }
}

