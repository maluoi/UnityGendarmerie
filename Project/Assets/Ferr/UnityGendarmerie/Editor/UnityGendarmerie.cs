/* UnityGendarmerie, 7/12/2015
 * https://github.com/maluoi/UnityGendarmerie
 * Written by Nick Klingensmith (@koujaku)
 * 
 * Basic usage:
 * First, ensure you have gendarme on your computer! (https://github.com/spouliot/gendarme/downloads)
 * May also be good to be aware of the default ignore.txt file that should be at Assets/Ferr/UnityGendarmerie/ignore.txt
 * If you're on a Mac, and/or only grabbed the plain binaries folder, you may need to manually specify your gendarme path in the configuration.txt!
 *
 * After that, it's all menus!
 * Use Tools->Ferr UnityGendarmerie->Run Static Code Analysis (Runtime code) 
 *	to analyze files that are distributed with your game, basically most things that aren't in the Editor folders
 * Use Tools->Ferr UnityGendarmerie->Run Static Code Analysis (Editor code)
 *	to analyze your custom editor code!
 * Right Click->Ferr UnityGendarmerie->Analyze Code on a .cs file, or a folder
 *	To analyze specific files, or folders of files. Use the warning levels to filter out lower level warnings.
 * 
 * Want better access to the data, or want to analyze other assemblies? Fret not!
 * UnityGendarmerie.AnalyzeCode(new Uri("filename.exe"), true);
 * AnalyzeCode has options for specifying specific assemblies, filters, and custom ignore files! It also returns
 * a list of the data in case you're interested in logging or doing something else with it yourself!
 * Use AnalyzeCodePath to do the same on a specific file or folder, this will only work for files that fit
 * into the runtime and editor assemblies!
 * 
 * Also, here's an example ignore file, if you need some reference!
 * https://github.com/mono/mono-tools/blob/master/gendarme/self-test.ignore
 *
 * The MIT License (MIT)
 * 
 * Copyright (c) 2015 Nick Klingensmith
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * */

using UnityEditor;
using UnityEngine;

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Debug = UnityEngine.Debug;

namespace Ferr {
	
	public enum GendarmeSeverity {
		Audit,
		Low,
		Medium,
		High,
		Critical
	}
	public enum GendarmeConfidence {
		Low,
		Normal,
		High,
		Total
	}
	
	public class AnalysisItem {
		public string             type       {get;set;}
		public GendarmeSeverity   severity   {get;set;}
		public GendarmeConfidence confidence {get;set;}
		public string             target     {get;set;}
		public string             source     {get;set;}
		public int                line       {get;set;}
		public string             details    {get;set;}
		public string             solution   {get;set;}
		public string             info       {get;set;}
	}
	
	public static class UnityGendarmerie {
		#region Setup
		const string configPath      = "Assets/Ferr/UnityGendarmerie/configuration.txt";
		const string editorAssembly  = "/Library/ScriptAssemblies/Assembly-CSharp-Editor.dll";
		const string runtimeAssembly = "/Library/ScriptAssemblies/Assembly-CSharp.dll";
		
		const string logFormat    = "Severity {1}: {0}\n{3}:{4}\n{2}\n\n{5} Solution: {6}\nFor more info: {7}\n";
		const int    menuPriority = 100;
		#endregion
		
		#region Config variables
		static string gendarmePath;
		static string defaultIgnore;
		#endregion
		
		#region Properties
		static string ProjectPath { get { return Application.dataPath + "/.."; } }
		#endregion
		
		#region Menus
		[MenuItem("Tools/Ferr Gendarme (Code Analysis)/Run Static Code Analysis (Runtime code)")]
		public static ICollection<AnalysisItem> AnalyzeRuntimeAssembly() {
			return AnalyzeCode(new Uri(ProjectPath+runtimeAssembly));
		}
		[MenuItem("Tools/Ferr Gendarme (Code Analysis)/Run Static Code Analysis (Editor code)")]
		public static ICollection<AnalysisItem> AnalyzeEditorAssembly() {
			return AnalyzeCode(new Uri(ProjectPath+editorAssembly));
		}
		[MenuItem("Assets/Ferr UnityGendarmerie/Analyze Code (Audit)",    false, menuPriority)] private static void AnalyzeFileAudit   () { AnalyzeCodePath(AssetDatabase.GetAssetPath(Selection.activeObject), true, null, GendarmeSeverity.Audit   ); }
		[MenuItem("Assets/Ferr UnityGendarmerie/Analyze Code (Low)",      false, menuPriority)] private static void AnalyzeFileLow     () { AnalyzeCodePath(AssetDatabase.GetAssetPath(Selection.activeObject), true, null, GendarmeSeverity.Low     ); }
		[MenuItem("Assets/Ferr UnityGendarmerie/Analyze Code (Medium)",   false, menuPriority)] private static void AnalyzeFileMedium  () { AnalyzeCodePath(AssetDatabase.GetAssetPath(Selection.activeObject), true, null, GendarmeSeverity.Medium  ); }
		[MenuItem("Assets/Ferr UnityGendarmerie/Analyze Code (High)",     false, menuPriority)] private static void AnalyzeFileHigh    () { AnalyzeCodePath(AssetDatabase.GetAssetPath(Selection.activeObject), true, null, GendarmeSeverity.High    ); }
		[MenuItem("Assets/Ferr UnityGendarmerie/Analyze Code (Critical)", false, menuPriority)] private static void AnalyzeFileCritical() { AnalyzeCodePath(AssetDatabase.GetAssetPath(Selection.activeObject), true, null, GendarmeSeverity.Critical); }
		[MenuItem("Assets/Ferr UnityGendarmerie/Analyze Code (Audit)",    true,  menuPriority)]
		[MenuItem("Assets/Ferr UnityGendarmerie/Analyze Code (Low)",      true,  menuPriority)] 
		[MenuItem("Assets/Ferr UnityGendarmerie/Analyze Code (Medium)",   true,  menuPriority)] 
		[MenuItem("Assets/Ferr UnityGendarmerie/Analyze Code (High)",     true,  menuPriority)] 
		[MenuItem("Assets/Ferr UnityGendarmerie/Analyze Code (Critical)", true,  menuPriority)] private static bool AnalyzeFileAuditValidate() {return IsCodeOrFolder(Selection.activeObject);}
		#endregion
		
		#region Public Methods
		/// <summary>
		/// Uses Gendarme to do a static code analysis of your assembly! This does require that Gendarme is installed on your computer (https://github.com/spouliot/gendarme/downloads)
		/// and may require you to tweak the configuration file!
		/// </summary>
		/// <param name="aAssembly">Path to an assembly. Any .exe, .dll or whatever! Preferably with debug symbols present.</param>
		/// <param name="aLog">Want this method to log the info for you? Sure!</param>
		/// <param name="aIgnoreFile">If you want to use a custom ignore file, provide it here, otherwise it'll just pull from the example/default ignore file. For examples: https://github.com/mono/mono-tools/blob/master/gendarme/self-test.ignore</param>
		/// <param name="aFilter">What's the lowest severity item you want to see in the analysis? Anything below this is ignored.</param>
		/// <returns>
		/// Returns all items scraped from the analysis, or an empty list on failure. Fields not present in the analysis 
		/// will be empty strings in the list data.
		/// </returns>
		public static ICollection<AnalysisItem> AnalyzeCode(Uri aAssembly, bool aLog = true, Uri aIgnoreFile = null, GendarmeSeverity aFilter = GendarmeSeverity.Medium) {
			if (aAssembly == null) return new List<AnalysisItem>();
			if (!LoadConfig(configPath)) {
				Debug.LogError(string.Format("Config file for UnityGendarmerie was not found at {0}!", configPath));
				return new List<AnalysisItem>();
			}
			if (!GendarmePresent()) {
				Debug.LogError(string.Format("Gendarme was not found at {0}! You may wish to install it, and set the correct path in the configuration file! https://github.com/spouliot/gendarme/downloads", gendarmePath));
				return new List<AnalysisItem>();
			}
			
			List<AnalysisItem> items = null;
			
			string ignoreFile = aIgnoreFile != null? aIgnoreFile.AbsolutePath : new Uri(ProjectPath+defaultIgnore).AbsolutePath;
			
			using (Process proc = new Process()) {
				ProcessStartInfo info = proc.StartInfo;
				info.FileName               = gendarmePath;
				info.WorkingDirectory       = ProjectPath;
				info.Arguments              = string.Format("--ignore '{0}' --severity {1}+ '{2}'", ignoreFile, aFilter, aAssembly.AbsolutePath).Replace('\'', '"');
				info.UseShellExecute        = false;
				info.RedirectStandardOutput = true;
				info.CreateNoWindow         = true;
				info.RedirectStandardError  = true;
				
				proc.Start();
				
				items = ScrapeAnalysis(proc.StandardOutput.ReadToEnd());
				if (aLog) LogItems(items);
				
				string err = proc.StandardError.ReadToEnd();
				if (!string.IsNullOrEmpty(err))
					Debug.LogError(err);
			}
			return items;
		}
		/// <summary>
		/// Uses Gendarme to do a static code analysis on a specific file! (through filtering) Only works on files in either the editor or runtime assemblies. 
		/// This does require that Gendarme is installed on your computer (https://github.com/spouliot/gendarme/downloads) and may require you to tweak the 
		/// configuration file!
		/// </summary>
		/// <param name="aFileName">Path to a code file. Can be relative or absolute.</param>
		/// <param name="aLog">Want this method to log the info for you? Sure!</param>
		/// <param name="aIgnoreFile">If you want to use a custom ignore file, provide it here, otherwise it'll just pull from the example/default ignore file. For examples: https://github.com/mono/mono-tools/blob/master/gendarme/self-test.ignore</param>
		/// <param name="aFilter">What's the lowest severity item you want to see in the analysis? Anything below this is ignored.</param>
		/// <returns>
		/// Returns all items scraped from the analysis, or an empty list on failure. Fields not present in the analysis 
		/// will be empty strings in the list data.
		/// </returns>
		public static ICollection<AnalysisItem> AnalyzeCodePath(string aFileName, bool aLog = true, Uri aIgnoreFile = null, GendarmeSeverity aFilter = GendarmeSeverity.Medium) {
			if (!File.Exists(aFileName) && !Directory.Exists(aFileName)) return new List<AnalysisItem>();
			
			// identify which assembly we need to look in for the file
			string assembly = runtimeAssembly;
			if (aFileName.IndexOf(@"/Editor/", StringComparison.OrdinalIgnoreCase) >= 0 || aFileName.IndexOf(@"\Editor\", StringComparison.OrdinalIgnoreCase) >= 0){
				assembly = editorAssembly;
			}
			
			// get the items, and filter them based on what source it's from
			Uri                       assemblyPath = new Uri(ProjectPath+assembly);
			Uri                       filePath     = new Uri(Path.GetFullPath(aFileName));
			ICollection<AnalysisItem> items        = AnalyzeCode(assemblyPath, false, aIgnoreFile, aFilter);
			List<AnalysisItem>        finalItems   = new List<AnalysisItem>();
			foreach(AnalysisItem item in items) {
				if (!string.IsNullOrEmpty(item.source)) {
					if (new Uri(item.source).ToString().StartsWith(filePath.ToString())) finalItems.Add(item);
				}
			}
			
			if (aLog) LogItems(finalItems);
			
			return finalItems;
		}
		#endregion
		
		#region Private Methods
		static void LogItems(List<AnalysisItem> aItems) {
			Debug.ClearDeveloperConsole();
			for (int i=0; i<aItems.Count; i+=1) {
				AnalysisItem it      = aItems[i];
				string       source  = it.source;
				if (source.StartsWith(Application.dataPath)) source = source.Substring(Application.dataPath.Length);
				string       message = string.Format(logFormat, it.type, it.severity, it.target, source, it.line, it.details, it.solution, it.info);
				
				if      (it.severity == GendarmeSeverity.Critical || (it.severity == GendarmeSeverity.High && it.confidence == GendarmeConfidence.Total)) Debug.LogError(message);
				else if (it.severity == GendarmeSeverity.High) Debug.LogWarning(message);
				else Debug.Log(message);
			}
			
			if (aItems.Count <= 0)
				Debug.Log("No issues found!");
		}
		static List<AnalysisItem> ScrapeAnalysis(string aAnalysis) {
			List<AnalysisItem> result = new List<AnalysisItem>();
			
			Regex item           = new Regex(@"[\r\n]\d+\.([\s\S]*?)(?=[\r\n]\d+\.|Processed)", RegexOptions.Multiline );
			Regex itemType       = new Regex(@"[\r\n]\d+\.\s*(.*)"  );
			Regex itemSeverity   = new Regex(@"Severity:\s*(.*?),"  );
			Regex itemConfidence = new Regex(@"Confidence:\s*(.*)"  );
			Regex itemTarget     = new Regex(@"Target:\s*(.*)"      );
			Regex itemSource     = new Regex(@"Source:\s*(.*)(?=\()");
			Regex itemLine       = new Regex(@"Source:.*\(.(\d*)\)" );
			Regex itemDetails    = new Regex(@"Details:\s*(.*)"     );
			Regex itemSolution   = new Regex(@"Solution:\s*(.*)"    );
			Regex itemInfo       = new Regex(@"available at:\s*(.*)");
			
			var matches = item.Matches(aAnalysis);
			foreach(Match match in matches) {
				string itemString = match.Groups[0].Value;
				
				AnalysisItem scrape = new AnalysisItem();
				scrape.type         = itemType      .Match(itemString).Groups[1].Value;
				scrape.target       = itemTarget    .Match(itemString).Groups[1].Value;
				scrape.source       = itemSource    .Match(itemString).Groups[1].Value;
				scrape.details      = itemDetails   .Match(itemString).Groups[1].Value;
				scrape.solution     = itemSolution  .Match(itemString).Groups[1].Value;
				scrape.info         = itemInfo      .Match(itemString).Groups[1].Value;
				
				string severity     = itemSeverity  .Match(itemString).Groups[1].Value;
				string confidence   = itemConfidence.Match(itemString).Groups[1].Value;
				string line         = itemLine      .Match(itemString).Groups[1].Value;
				
				try { scrape.severity   = (GendarmeSeverity  )Enum.Parse(typeof(GendarmeSeverity  ), severity  ); } catch {}
				try { scrape.confidence = (GendarmeConfidence)Enum.Parse(typeof(GendarmeConfidence), confidence); } catch {}
				
				int lineVal = 0;
				int.TryParse(line, out lineVal);
				scrape.line = lineVal;
				
				result.Add(scrape);
			}
			return result;
		}
		static bool LoadConfig(string aFile) {
			if (!File.Exists(aFile)) {
				return false;
			}
			
			Regex configItem = new Regex(@"\[\s*(\w*)\s*\]\s*(.*)");
			
			using (StreamReader reader = new StreamReader(aFile)) {
				while (!reader.EndOfStream) {
					string          line   = reader.ReadLine();
					GroupCollection groups = configItem.Match(line).Groups;
					
					string name  = groups[1].Value.ToLower();
					string value = groups[2].Value;
					
					if      (name == "gendarmepath" ) { gendarmePath  = value; } 
					else if (name == "defaultignore") { defaultIgnore = value; }
				}
				reader.Close();
			}
			return true;
		}
		static bool IsCodeOrFolder(UnityEngine.Object aObj) {
			if (aObj == null) return false;
			
			if (aObj.GetType() == typeof(UnityEngine.Object)) {
				if (string.IsNullOrEmpty(Path.GetExtension(AssetDatabase.GetAssetPath(aObj)))) {
					return true;
				}
			}
			
			if (aObj != null) {
				string path = AssetDatabase.GetAssetPath(aObj);
				if (!string.IsNullOrEmpty(path))
					return path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
		static bool GendarmePresent() {
			return File.Exists(gendarmePath);
		}
		#endregion
	}
}