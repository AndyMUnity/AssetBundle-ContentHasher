using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace BundleHashing
{
	/// <summary>
	/// 
	/// </summary>
	public class AssetBundleUnpacker
	{
		private static List<AssetBundleUnpacker> m_Active = new List<AssetBundleUnpacker>();
		
		private List<FileInfo> m_AssetBundleFiles;
		private List<DirectoryInfo> m_UnpackedDirectories;
		/// <summary>
		/// 
		/// </summary>
		public List<DirectoryInfo> UnpackedDirectories
		{
			get { return m_UnpackedDirectories; }
		}
		
		private Process m_Process = null;
		private bool m_HasStarted = false;
		private int m_FileIndex = 0;

		private bool m_IsDone = false;
		/// <summary>
		/// 
		/// </summary>
		public bool IsDone
		{
			get { return m_IsDone; }
		}

		/// <summary>
		/// 
		/// </summary>
		public Action OnCompleted;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="assetBundleFilePaths"></param>
		public AssetBundleUnpacker( IList<string> assetBundleFilePaths )
		{
			m_AssetBundleFiles = new List<FileInfo>( assetBundleFilePaths.Count );
			foreach( string filePath in assetBundleFilePaths )
				m_AssetBundleFiles.Add( new FileInfo( filePath ) );
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="assetBundleFiles"></param>
		public AssetBundleUnpacker( IList<FileInfo> assetBundleFiles )
		{
			m_AssetBundleFiles = new List<FileInfo>(assetBundleFiles);
		}

		/// <summary>
		/// 
		/// </summary>
		public void Start()
		{
			if( m_HasStarted || m_IsDone )
				return;

			if( m_AssetBundleFiles.Count == 0 )
				m_IsDone = true;
			m_UnpackedDirectories = new List<DirectoryInfo>( m_AssetBundleFiles.Count );
			for( int i=0; i<m_AssetBundleFiles.Count; ++i )
				m_UnpackedDirectories.Add( null );
			
			if( m_AssetBundleFiles.Count == 0 )
			{
				OnCompleted?.Invoke();
				return;
			}

			m_HasStarted = true;
			if( m_Active.Count == 0 )
				EditorApplication.update += UpdateEditor;

			m_Active.Add( this );
			m_FileIndex = 0;
			UnpackAssetBundleFile( m_AssetBundleFiles[m_FileIndex] );
		}

		private static void UpdateEditor()
		{
			for( int i = m_Active.Count - 1; i >= 0; --i )
				m_Active[i].Update();
		}

		// May be able to run multiple processes, but I expect little gain
		private void Update()
		{
			if( m_Process == null )
			{
				if( m_FileIndex < m_AssetBundleFiles.Count )
				{
					UnpackAssetBundleFile( m_AssetBundleFiles[m_FileIndex] );
				}
				else
				{
					m_Active.Remove( this );
					if( m_Active.Count == 0 )
					{
						EditorApplication.update -= UpdateEditor;
						EditorUtility.ClearProgressBar();
					}

					m_IsDone = true;
					OnCompleted?.Invoke();
				}
			}
		}

		private void UnpackAssetBundleFile( FileInfo file )
		{
			EditorUtility.DisplayProgressBar( "Unpacking AssetBundle for hashing", file.FullName, (float) m_FileIndex / m_AssetBundleFiles.Count );
			m_Process = new Process
			{
				EnableRaisingEvents = true
			};

			m_Process.StartInfo.FileName = EditorApplication.applicationPath + "/Contents/Tools/WebExtract";
			m_Process.StartInfo.Arguments = "\"" + file.FullName + "\"";
			m_Process.StartInfo.UseShellExecute = false;
			m_Process.StartInfo.RedirectStandardOutput = true;

			m_Process.Exited += WebExtractProcess_Exited;
			m_Process.OutputDataReceived += WebExtractProcess_Output;

			try
			{
				m_Process.Start();
				m_Process.BeginOutputReadLine();
			}
			catch( Exception e )
			{
				Debug.LogError( "Failed to start process : " + e.Message );
			}
		}

		private void WebExtractProcess_Exited( object sender, EventArgs e )
		{
			m_Process.Close();
			m_Process.Dispose();
			m_Process = null;
			m_FileIndex++;
		}

		private void WebExtractProcess_Output( object sender, DataReceivedEventArgs e )
		{
			if( string.IsNullOrEmpty( e.Data ) )
				return;

			if( e.Data.Contains( "creating folder" ) )
			{
				var split = e.Data.Split( '\'' );
				if( Directory.Exists( split[1] ) == false )
					Debug.LogError( "Failed to find unpacked folder" );
				else
					m_UnpackedDirectories[m_FileIndex] = new DirectoryInfo( split[1] );
			}
		}
	}
}
