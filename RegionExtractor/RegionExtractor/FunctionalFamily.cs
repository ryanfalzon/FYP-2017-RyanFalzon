﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class FunctionalFamily
    {
        // Private properties
        private string funfam;
        private string consensusSequence;
        private List<Kmer> kmers;

        // Getters and setters
        public string Funfam { get => funfam; set => funfam = value; }
        public string ConsensusSequence { get => consensusSequence; set => consensusSequence = value; }
        internal List<Kmer> Kmers { get => kmers; set => kmers = value; }

        // COnstructor 1
        public FunctionalFamily(string funfam)
        {
            this.funfam = funfam;
            this.kmers = new List<Kmer>();
        }

        // Constructor 2
        public FunctionalFamily(string funfam, string consensusSequence)
        {
            this.funfam = funfam;
            this.consensusSequence = consensusSequence;
            this.kmers = new List<Kmer>();
        }
    }
}
