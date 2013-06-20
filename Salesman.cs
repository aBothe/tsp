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
using System.Linq;

namespace tsp
{
	class Salesman
	{
		bool showProgress = true;
		int n;
		string inputFile;
		string[] cityNames;
		float[][] inputMatrix;

		public static void Main (string[] args)
		{
			if(args.Length < 1)
				args = new[] { "afrika.txt" };
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
			//t = salesman.CalcViaNearestInsertion (0);	salesman.OutputCityNames (t);

			//return;
			Console.WriteLine ("Nearest Insertion:\n");
			salesman.showProgress = false;

			for(int k = 0; k < salesman.n; k++){
				t = salesman.CalcViaNearestInsertion (k);
				salesman.OutputCityNames (t);
			}


			return;
			float weight;
			t = salesman.CalcViaNearestInsertion(3);

			t = salesman.CalcViaBranchAndBound (250/*weight*/);
		}

		void ReadInputMatrix()
		{
			var lines = File.ReadAllLines (inputFile);
			int lineIndex = 0;
			var splitChar = new[] { ' ' };

			n = Convert.ToInt32 (lines[lineIndex++]);
			cityNames = lines [lineIndex++].Split (splitChar,StringSplitOptions.RemoveEmptyEntries);
			inputMatrix = new float[n][];

			for (int i=0; i < n; i++) {
				var splits = lines [lineIndex++].Split (splitChar,StringSplitOptions.RemoveEmptyEntries);
				inputMatrix [i] = new float[n];
				for (int k = 0; k < n; k++)
					inputMatrix [i] [k] = Convert.ToSingle (splits[k]);
			}
		}

		public float GetTourWeight(IEnumerable<int> tour)
		{
			float w = 0;

			var en = tour.GetEnumerator ();
			if(en.MoveNext())
				while(true){
						var currentTown = en.Current;
						if (!en.MoveNext ())
							break;

						w += inputMatrix [currentTown] [en.Current];
				}
			en.Dispose ();

			return w;
		}

		public static void DumpGraph(float[][] graph, int padding = 2)
		{
			var formatString = "{0:"+padding+"} ";
			foreach(var row in graph)
			{
				foreach(var v in row)
					Console.Write(formatString, v);
				Console.WriteLine();
			}
		}

		public void OutputCityNames(IEnumerable<int> tour, bool weight=true)
		{
			var en = tour.GetEnumerator ();
			if(en.MoveNext()){
				while(true){
					Console.Write (cityNames[en.Current]);
					if (!en.MoveNext ())
						break;

					Console.Write ("->");
				}
			}
			en.Dispose ();

			if(weight)
				Console.WriteLine(" (Weight: "+GetTourWeight (tour)+")");

			Console.WriteLine();
		}

		#region Heuristic approaches
		public int[] CalcViaNearestInsertion(int startCity = 0)
		{
			const string formatStr = "{0,5}";

			if (showProgress) {
				Console.WriteLine ("Start Nearest Insertion @ " + cityNames[startCity]);
			}

			int currentCity = startCity;
			var tourResult = new List<int>(n);
			tourResult.Add(startCity);
			tourResult.Add(startCity);
			var vertexUsed=new BitArray(n);

			var minDistances = inputMatrix [startCity].Clone () as float[];

			for(int i=0; i < n-1; i++)
			{
				// Als 'besucht' markieren
				vertexUsed [currentCity] = true;

				if (showProgress) {
					Console.WriteLine ();
					Console.ForegroundColor = ConsoleColor.Blue;
					Console.WriteLine ("Iteration "+(i+1).ToString()+":");

					// Dump city names
					Console.ForegroundColor = ConsoleColor.Black;
					Console.Write ("\t");
					for (int k = 0; k < n; k++)
						if(!vertexUsed[k])
							Console.Write (string.Format(formatStr,cityNames[k]));
					Console.WriteLine ();

					// Dump alt
					Console.Write ("alt\t");
					for (int k = 0; k < n; k++)
						if(!vertexUsed[k])
							Console.Write (string.Format(formatStr,minDistances[k]));
					Console.WriteLine ();

					// Dump current city
					Console.Write (cityNames[currentCity]+"\t");
					for (int k = 0; k < n; k++)
						if(!vertexUsed[k])
							Console.Write (string.Format(formatStr,inputMatrix[currentCity][k]));
					Console.WriteLine ();
				}

				int nextCity=-1;
				float tempMin = float.PositiveInfinity;

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


				if (nextCity < 0) {
					if (i < n - 1)
						throw new InvalidDataException ("nextCityIndex was not set.");
					else
						break;
				}
				currentCity = nextCity;

				if (showProgress) {
					// Dump min
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write ("min\t");
					for (int k = 0; k < n; k++)
						if(!vertexUsed[k])
							Console.Write (string.Format(formatStr,minDistances[k]));
					Console.WriteLine ();
					Console.ForegroundColor = ConsoleColor.Black;
				}

				// Station setzen
				if (showProgress) {
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.WriteLine ();
					Console.WriteLine ("Nächste Station setzen..");
					Console.ForegroundColor = ConsoleColor.Black;
				}

				float statMin=float.PositiveInfinity;
				int insertionIndex = 0;
				for(int j = 0; j<tourResult.Count-1;j++)
				{
					var A = tourResult [j];
					var C = tourResult [j+1];
					var delta = inputMatrix [A] [nextCity] + inputMatrix[nextCity][C] - inputMatrix [A] [C];

					if (showProgress) {
						Console.Write ("("+cityNames[A]+"->"+cityNames[C]+")="+inputMatrix [A] [C]);
						Console.Write ("\t");
						Console.Write ("("+cityNames[A]+"->"+cityNames[nextCity] +"->"+cityNames[C]+")="+(inputMatrix [A] [nextCity] + inputMatrix[nextCity][C]));
						Console.WriteLine("\tdelta="+delta);
					}

					if (statMin > delta) {
						statMin = delta;
						insertionIndex = j+1;
					}
				}

				if(showProgress){
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("min(delta)="+statMin+" => Insert "+cityNames[nextCity]+" between "+ cityNames[tourResult[insertionIndex-1]] + " and "+cityNames[tourResult[insertionIndex]]);
				}

				tourResult.Insert(insertionIndex,nextCity);

				if (showProgress) {
					Console.ForegroundColor = ConsoleColor.Green;
					OutputCityNames (tourResult);
					Console.ResetColor ();
				}
			}

			return tourResult.ToArray();
		}
		#endregion

		#region BnB
		public int[] CalcViaBranchAndBound(float maxWeight, int iterationLimit = 0)
		{
			if (iterationLimit <= 0)
				iterationLimit = n;

			var tour = new List<int>(n);
			var graph = inputMatrix.Clone () as float[][];
			var origIndexes = new List<int> (n);
			float minWeight = 0;

			// Initial preparation
			for (int i = 0; i < n; i++){
				graph [i] [i] = float.PositiveInfinity;
				origIndexes.Add (i);
			}

			initiallyReduceByMinimum (graph, maxWeight, ref minWeight);

			int t_y, t_x;
			float maxRemovableValue;
			searchNextPointtoRemove (graph, out t_y, out t_x, out maxRemovableValue);



			graph [t_y] [t_x] = float.PositiveInfinity;

			initiallyReduceByMinimum(graph, maxWeight, ref minWeight);


			/*
			var back = graph [t_x] [t_y];
			graph [t_x] [t_y] = float.PositiveInfinity;

			int n_y, n_x;
			if (!initiallyReduceByMinimum (graph, maxWeight, ref minWeight, out n_y, out n_x)) {
				graph [t_x] [t_y] = back;
			}
		

			var ret = bnb (graph, origIndexes, tour, ref minWeight, ref maxWeight);

			if (ret == null)
				return new int[0];
			return ret.ToArray();*/

			return new int[0];

		}

		static void bnb(float[][] graph, float minWeight, float maxWeight)
		{
			// Spalten/Zeilen auf mind. einen Nullwert reduzieren, das Mindestgewicht aufaddieren
			initiallyReduceByMinimum (graph, maxWeight, ref minWeight);

			// Eine Kante heraussuchen, die bei Verbot eine höchste untere Schranke hervorruft
			float maxRemovableValue;
			int y, x;
			searchNextPointtoRemove(graph, out y, out x, out maxRemovableValue);

			// Zuerst prüfen, ob Rückkante

			// Die erste Tour enthält (y,x)
			var back = graph [x] [y];
			graph [x] [y] = float.PositiveInfinity; // ohne Rückkante (x,y)
			var firstBranch = cloneGraph (graph, y, x);
			graph [x] [y] = back;



			/*
			var firstTour = new Tuple<int,int>[tour.Length+1];
			tour.CopyTo (firstTour, 0);
			firstTour [firstTour.Length-1] = new Tuple<int, int> (y, x);*/

			// Die zweite Menge enthält (y,x) nicht
			var secondRangeMinWeight = minWeight;
			var secondGraph = graph.Clone () as float[][];
			secondGraph [y] [x] = float.PositiveInfinity;

			int y2, x2;
			//initiallyReduceByMinimum (secondGraph, maxWeight, ref secondRangeMinWeight, out y2, out x2);
		}
		/*
		static List<int> bnb(float[][] graph, List<int> originalIndexes, List<int> optTour, ref float minWeight, ref float maxWeight)
		{
			float minWeight_withoutEdge;
			List<int> optTour_withoutEdge;
			float back;
			if (graph.Length == 0)
				return optTour;

			int pointToRemoveNext_y, pointToRemoveNext_x;
			if (!initiallyReduceByMinimum (graph, maxWeight, ref minWeight, out pointToRemoveNext_y, out pointToRemoveNext_x)) 
				return null;

			if(pointToRemoveNext_x == -1){
				float maxRemovableVal;
				searchNextPointtoRemove (graph, out pointToRemoveNext_x, out pointToRemoveNext_y, out maxRemovableVal);

				if (minWeight + maxRemovableVal > maxWeight)
					return null;
			}

			// Ohne Kante berechnen..
			back = graph [pointToRemoveNext_y] [pointToRemoveNext_x];
			graph [pointToRemoveNext_y] [pointToRemoveNext_x] = float.PositiveInfinity;
			minWeight_withoutEdge = minWeight;
			optTour_withoutEdge = bnb (graph.Clone() as float[][], originalIndexes, new List<int> (optTour), ref minWeight_withoutEdge, ref maxWeight);

			graph [pointToRemoveNext_y] [pointToRemoveNext_x] = back;


			// Die Rücktour verbieten
			back = graph [pointToRemoveNext_x] [pointToRemoveNext_y];
			graph [pointToRemoveNext_x] [pointToRemoveNext_y] = float.PositiveInfinity;

			var reducedGraph = cloneGraph (graph, pointToRemoveNext_y, pointToRemoveNext_x);

			graph [pointToRemoveNext_x] [pointToRemoveNext_y] = back;


			var reducedIndexList = new List<int> (originalIndexes);
			optTour.Add (originalIndexes[pointToRemoveNext_x]);
			reducedIndexList.RemoveAt (pointToRemoveNext_x);

			var optTour_withEdge = bnb (reducedGraph, reducedIndexList, new List<int>(optTour), ref minWeight, ref maxWeight);

			if (minWeight_withoutEdge < minWeight){
				minWeight = minWeight_withoutEdge;
				return optTour_withoutEdge;
			}
			return optTour_withEdge;
		}*/

		static float[][] cloneGraph(float[][] g, int rowToOmit, int colToOmit)
		{
			int newDimension = g.Length - 1;
			var clonedGraph = new float[newDimension][];

			for(int i = newDimension-1;i>=0;i--)
			{
				var targetRow = g[rowToOmit >= i ? i + 1 : i];
				var row = clonedGraph[i] = new float[newDimension];

				for (int k = newDimension-1; k>=0; k--)
					row [k] = targetRow [colToOmit >= k ? k+1 : k];
			}

			return clonedGraph;
		}

		static void searchNextPointtoRemove(float[][] graph, out int row, out int col, out float maxRemovableValue)
		{
			maxRemovableValue = 0;
			row = 0;
			col = 0;

			/*
			 * graph enthält in jeder Spalte und in jeder Zeile mind. 1 Null!
			 * 
			 * 
			 */
			var i_max = graph.Length-1;

			for(int i = i_max;i>=0;i--)
			{
				var curRow = graph [i];
				bool hadZero = false;
				for(int k = i_max;k>=0;k--)
				{
					if (curRow[k] <= 0) {
						hadZero = true;

						float tempMin;

						if (hadZero)
							tempMin = 0;
						else{
							tempMin = float.PositiveInfinity;
							for (int c = i_max; c>=0; c--)
								if (c != k && curRow[c] < tempMin)
									tempMin = curRow[c];
						}

						var tempMin2 = float.PositiveInfinity;
						for (int c = i_max; c>=0; c--)
							if (c != i && graph [c] [k] < tempMin2)
								tempMin2 = graph [c] [k];

						if (tempMin + tempMin2 > maxRemovableValue) {
							row = i;
							col = k;
							maxRemovableValue = tempMin + tempMin2;
						}
					}
				}
			}
		}

		static bool initiallyReduceByMinimum(float[][] graph, float maxWeight, ref float minWeight)
		{
			var i_max = graph.Length -1;
			List<float> minBackups = null, minBackups_colBased = null;
			bool checkForMax = maxWeight >= 0 && maxWeight < float.PositiveInfinity;
			if (checkForMax)
				minBackups = new List<float> (graph.Length);
			/*
			minCoord_Row = -1;
			minCoord_Col = -1;
			*/var globalMin_y = 0;
			var globalMin_x = 0;
			float globalMin = 0;

			for (int i= i_max; i>=0; i--) {
				// Zeilenweises bestimmen des Minimums
				float tempMin = float.PositiveInfinity;
				for (int k = i_max; k >= 0; k--) {
					if (tempMin > graph [i] [k])
						tempMin = graph [globalMin_y = i] [globalMin_x = k];
				}

				if (checkForMax && minWeight + tempMin > maxWeight) {
					rollbackGraphMatrix(graph, ref minWeight, minBackups);
					return false;
				}

				// Zusammenrechnen der Minima
				minWeight += tempMin;

				if (checkForMax)
					minBackups.Add (tempMin);

				if (tempMin > 0) {
					if (tempMin > globalMin) {
						/*minCoord_Row = globalMin_y;
						minCoord_Col = globalMin_x;
						*/globalMin = tempMin;
					}
					// Abzug der jeweiligen Minima von den Kantenmarkierungen
					for (int k = graph.Length -1; k >= 0; k--)
						graph [i] [k] -= tempMin;
				}
			}

			if (checkForMax)
				minBackups_colBased = new List<float> (graph.Length);

			for (int i= i_max; i>=0; i--) {
				// Spaltenweises bestimmen des Minimums
				float tempMin = float.PositiveInfinity;
				for (int k = i_max; k >= 0; k--) {
					if (tempMin > graph [k][i]){
						tempMin = graph [globalMin_y = k] [globalMin_x = i];
					}
				}

				if (checkForMax && minWeight + tempMin > maxWeight) {
					rollbackGraphMatrix(graph, ref minWeight, minBackups);
					rollbackGraphMatrix_ColumnBased (graph, ref minWeight, minBackups_colBased);
					return false;
				}

				// Zusammenrechnen der Minima
				minWeight += tempMin;

				if (checkForMax)
					minBackups_colBased.Add (tempMin);

				if (tempMin > 0) {
					if (tempMin > globalMin) {
						/*minCoord_Row = globalMin_y;
						minCoord_Col = globalMin_x;
						*/globalMin = tempMin;
					}

					// Abzug der jeweiligen Minima von den Kantenmarkierungen
					for (int k = i_max; k >= 0; k--)
						graph [k][i] -= tempMin;
				}
			}

			return true;
		}

		static void rollbackGraphMatrix(float[][] graph, ref float minWeight, List<float> minBackups)
		{
			if (minBackups.Count > graph.Length)
				throw new InvalidOperationException ("minBackups muss weniger oder gleich viele Elemente als die Inputmatrix enthalten");

			for(int i = minBackups.Count -1; i >= 0; i--)
			{
				var tempMin = minBackups[i];
				minWeight -= tempMin;
				var row = graph [i];
				for (int k = row.Length -1; k>=0; k--)
					row [k] += tempMin;
			}
		}

		static void rollbackGraphMatrix_ColumnBased(float[][] graph, ref float minWeight, List<float> minBackups)
		{
			if (minBackups.Count > graph.Length)
				throw new InvalidOperationException ("minBackups muss weniger oder gleich viele Elemente als die Inputmatrix enthalten");

			for(int i = minBackups.Count -1; i >= 0; i--)
			{
				var tempMin = minBackups[i];
				minWeight -= tempMin;
				for (int k = graph.Length -1; k>=0; k--)
					graph[k][i] += tempMin;
			}
		}
		#endregion
	}
}
