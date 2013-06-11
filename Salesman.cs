//
// Program.cs
//
// Author:
//       Alexander Bothe <info@alexanderbothe.com>
//
// Copyright (c) 2013 Alexander Bothe
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace tsp
{
	class Salesman
	{
		bool showProgress;
		int n;
		string inputFile;
		string[] cityNames;
		float[][] inputMatrix;

		public static void Main (string[] args)
		{
			args = new[] { "ger.txt" };
			var salesman = new Salesman ();

			for(int i = 0; i < args.Length; i++)
			{
				var arg = args [i];
				if (arg[0] == '-') {
					arg = arg.TrimStart ('-');
					switch (arg) {
						case "progress":
						case "p":
							salesman.showProgress = true;
							break;
						default:
							Console.Error.WriteLine ("Invalid option: "+arg);
							return;
					}
				} else {
					salesman.inputFile = arg;
				}
			}

			if (args.Length < 1) {
				Console.WriteLine ("TSP solver.\nUsage: tsp [-progress] inputMatrixFile");
				return;
			}

			salesman.ReadInputMatrix ();

			int[] t;

			/*
			Console.WriteLine ("Nearest Insertion:\n");

			for(int k = 0; k < salesman.n; k++){
				t = salesman.CalcViaNearestInsertion (k);
				salesman.OutputCityNames (t);
			}

			Console.WriteLine ("Nearest City:\n");

			for(int k = 0; k < salesman.n; k++){
				float w;
				t = salesman.CalcViaNextCity (out w,k);
				Console.Write (w.ToString()+"\t");
				salesman.OutputCityNames (t);
			}*/
			float weight;
			t = salesman.CalcViaNextCity(out weight,3);

			t = salesman.CalcViaBranchAndBound (weight);
		}

		void ReadInputMatrix()
		{
			var lines = File.ReadAllLines (inputFile);
			int lineIndex = 0;

			n = Convert.ToInt32 (lines[lineIndex++]);
			cityNames = lines [lineIndex++].Split (' ');
			inputMatrix = new float[n][];

			for (int i=0; i < n; i++) {
				var splits = lines [lineIndex++].Split (' ');
				inputMatrix [i] = new float[n];
				for (int k = 0; k < n; k++)
					inputMatrix [i] [k] = Convert.ToSingle (splits[k]);
			}
		}

		public float GetTourWeight(int[] tour)
		{
			var n = tour.Length;
			var currentTown = tour [n-1];
			var nextTown = tour [0];

			var w=inputMatrix[currentTown][nextTown];


			for (var i=0; i<n-1; i++) {
				currentTown = tour [i];
				nextTown = tour [i+1];

				w += inputMatrix [currentTown] [nextTown];
			}

			return w;
		}

		public void OutputCityNames(int[] tour, bool weight=true)
		{
			foreach (var i in tour) {
				Console.Write (cityNames[i]);
				Console.Write ("->");
			}
			Console.WriteLine (cityNames[tour[0]]);

			if(weight)
				Console.WriteLine("(Weight: "+GetTourWeight (tour)+")");
		}

		#region Heuristic approaches
		public int[] CalcViaNearestInsertion(int startCity = 0)
		{
			int currentCity = startCity;
			var tourResult = new int[n];
			var vertexUsed=new BitArray(n);

			float[] minDistances = inputMatrix [startCity].Clone () as float[];

			for(int i=0; i < n; i++)
			{
				// Station setzen
				tourResult [i] = currentCity;
				// Als 'besucht' markieren
				vertexUsed [currentCity] = true;

				int nextCity=-1;
				float tempMin = int.MaxValue;

				for (int k = 0; k<n; k++) {
					if (vertexUsed [k])
						continue;

					if (inputMatrix [currentCity] [k] < minDistances [k]) {
						minDistances [k] = inputMatrix [currentCity] [k];
					}

					if (minDistances [k] < tempMin) {
						tempMin = minDistances [k];
						nextCity = k;
					}
				}


				if (nextCity < 0 && i < n - 1)
					throw new InvalidDataException ("nextCityIndex was not set.");
				currentCity = nextCity;
			}

			return tourResult;
		}

		public int[] CalcViaNextCity(out float weight,int startCity=0)
		{
			int currentCity = startCity;
			weight=0;
			var tourResult = new int[n];
			var vertexUsed=new BitArray(n);

			for(int i=0; i < n; i++)
			{
				// Station setzen
				tourResult [i] = currentCity;
				// Als 'besucht' markieren
				vertexUsed [currentCity] = true;

				int nextCity=-1;
				float tempDist = float.MaxValue;

				for (int k=0; k < n; k++) {
					// Stadt ignorieren, wenn bereits besucht oder weiter entfernt als zu 'nextCity'
					if (vertexUsed [k] || 
					    inputMatrix[currentCity][k] >= tempDist)
						continue;

					// Eine an Stadt wurde gefunden, die currentCity am nächsten ist.
					nextCity = k;
					tempDist = inputMatrix [currentCity] [k];
				}

				if (nextCity < 0) {
					if(i < n - 1)
						throw new InvalidDataException ("nextCityIndex was not set.");

					// Die Distanz der letzten Stadt zum Anfang addieren
					tempDist = inputMatrix [currentCity] [startCity];
				}

				weight += tempDist;
				currentCity = nextCity;
			}

			return tourResult;
		}
		#endregion

		#region BnB
		public int[] CalcViaBranchAndBound(float maxWeight, int iterationLimit = 0, bool keepInputMatrixUntouched = false)
		{
			if (iterationLimit <= 0)
				iterationLimit = n;

			var tour = new List<int>(n);
			var graph = keepInputMatrixUntouched ? inputMatrix.Clone () as float[][] : inputMatrix;

			// Initial preparation
			for (int i = n-1; i >= 0; i--)
				graph [i] [i] = float.PositiveInfinity;

			float minWeight = 0;
			int pointToRemoveNext_y, pointToRemoveNext_x;
			initiallyReduceByMinimum(graph, maxWeight, ref minWeight, out pointToRemoveNext_y, out pointToRemoveNext_x);


			return tour.ToArray ();
			/*
			while(pointToRemoveNext_y >= 0) {
				var backup = graph [pointToRemoveNext_y] [pointToRemoveNext_x];
				graph [pointToRemoveNext_y] [pointToRemoveNext_x] = float.PositiveInfinity;
				int pn_y, pn_x;
				if (initiallyReduceByMinimum (graph, maxWeight, ref minWeight, out pn_y, out pn_x)) {

				}
			}*/
		}

		static bool initiallyReduceByMinimum(float[][] graph, float maxWeight, ref float minWeight, out int minCoord_Row, out int minCoord_Col)
		{
			List<float> minBackups = null, minBackups_colBased = null;
			bool checkForMax = maxWeight >= 0 && maxWeight < float.PositiveInfinity;
			if (checkForMax)
				minBackups = new List<float> (graph.Length);

			minCoord_Row = -1;
			minCoord_Col = -1;
			var globalMin_y = 0;
			var globalMin_x = 0;
			float globalMin = 0;

			for (int i= graph.Length-1; i>=0; i--) {
				// Zeilenweises bestimmen des Minimums
				float tempMin = float.PositiveInfinity;
				for (int k = graph.Length -1; k >= 0; k--) {
					if (tempMin > graph [i] [k])
						tempMin = graph [globalMin_y = i] [globalMin_x = k];
				}

				if (checkForMax && minWeight + tempMin > maxWeight) {
					rollbackGraphMatrix(graph, minBackups);
					return false;
				}

				// Zusammenrechnen der Minima
				minWeight += tempMin;

				if (checkForMax)
					minBackups.Add (tempMin);

				if (tempMin > 0) {
					if (tempMin > globalMin) {
						minCoord_Row = globalMin_y;
						minCoord_Col = globalMin_x;
						globalMin = tempMin;
					}
					// Abzug der jeweiligen Minima von den Kantenmarkierungen
					for (int k = graph.Length -1; k >= 0; k--)
						graph [i] [k] -= tempMin;
				}
			}

			if (checkForMax)
				minBackups_colBased = new List<float> (graph.Length);

			for (int i= graph.Length-1; i>=0; i--) {
				// Spaltenweises bestimmen des Minimums
				float tempMin = float.PositiveInfinity;
				for (int k = graph.Length -1; k >= 0; k--) {
					if (tempMin > graph [k][i]){
						tempMin = graph [globalMin_y = k] [globalMin_x = i];
					}
				}

				if (checkForMax && minWeight + tempMin > maxWeight) {
					rollbackGraphMatrix(graph, minBackups);
					rollbackGraphMatrix_ColumnBased (graph, minBackups_colBased);
					return false;
				}

				// Zusammenrechnen der Minima
				minWeight += tempMin;

				if (checkForMax)
					minBackups_colBased.Add (tempMin);

				if (tempMin > 0) {
					if (tempMin > globalMin) {
						minCoord_Row = globalMin_y;
						minCoord_Col = globalMin_x;
						globalMin = tempMin;
					}

					// Abzug der jeweiligen Minima von den Kantenmarkierungen
					for (int k = graph.Length -1; k >= 0; k--)
						graph [k][i] -= tempMin;
				}
			}

			return true;
		}

		static void rollbackGraphMatrix(float[][] graph, List<float> minBackups)
		{
			if (minBackups.Count > graph.Length)
				throw new InvalidOperationException ("minBackups muss weniger oder gleich viele Elemente als die Inputmatrix enthalten");

			for(int i = minBackups.Count -1; i >= 0; i--)
			{
				var tempMin = minBackups[i];
				var row = graph [i];
				for (int k = row.Length -1; k>=0; k--)
					row [k] += tempMin;
			}
		}

		static void rollbackGraphMatrix_ColumnBased(float[][] graph, List<float> minBackups)
		{
			if (minBackups.Count > graph.Length)
				throw new InvalidOperationException ("minBackups muss weniger oder gleich viele Elemente als die Inputmatrix enthalten");

			for(int i = minBackups.Count -1; i >= 0; i--)
			{
				var tempMin = minBackups[i];
				for (int k = graph.Length -1; k>=0; k--)
					graph[k][i] += tempMin;
			}
		}
		#endregion
	}
}
