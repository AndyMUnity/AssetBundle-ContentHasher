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
	public class Binary2TextProcessor
	{
		private static List<Binary2TextProcessor> m_Active = new List<Binary2TextProcessor>();
		
		private List<FileInfo> m_BinaryFiles;
		private List<FileInfo> m_TextFiles;
		/// <summary>
		/// 
		/// </summary>
		public List<FileInfo> TextFiles
		{
			get { return m_TextFiles; }
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
		public Binary2TextProcessor( IList<string> assetBundleFilePaths )
		{
			m_BinaryFiles = new List<FileInfo>( assetBundleFilePaths.Count );
			foreach( string filePath in assetBundleFilePaths )
				m_BinaryFiles.Add( new FileInfo( filePath ) );
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="assetBundleFiles"></param>
		public Binary2TextProcessor( IList<FileInfo> assetBundleFiles )
		{
			m_BinaryFiles = new List<FileInfo>(assetBundleFiles);
		}

		/// <summary>
		/// 
		/// </summary>
		public void Start()
		{
			if( m_HasStarted || m_IsDone )
				return;
			
			if( m_BinaryFiles.Count == 0 )
				m_IsDone = true;
			m_TextFiles = new List<FileInfo>( m_BinaryFiles.Count );
			for( int i=0; i<m_BinaryFiles.Count; ++i )
				m_TextFiles.Add( null );
			
			if( m_BinaryFiles.Count == 0 )
			{
				OnCompleted?.Invoke();
				return;
			}

			m_HasStarted = true;
			if( m_Active.Count == 0 )
				EditorApplication.update += UpdateEditor;

			m_Active.Add( this );
			m_FileIndex = 0;
			ConvertBinaryFileToText( m_BinaryFiles[m_FileIndex] );
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
				if( m_FileIndex < m_BinaryFiles.Count )
				{
					ConvertBinaryFileToText( m_BinaryFiles[m_FileIndex] );
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
		
		private void ConvertBinaryFileToText( FileInfo file )
		{
			EditorUtility.DisplayProgressBar( "Converting to text for hashing", file.FullName, (float) m_FileIndex / m_BinaryFiles.Count );
			m_Process = new Process
			{
				EnableRaisingEvents = true
			};

			m_Process.StartInfo.FileName = EditorApplication.applicationPath + "/Contents/Tools/binary2text";
			m_Process.StartInfo.Arguments = "\"" + file.FullName + "\"";
			m_Process.StartInfo.UseShellExecute = false;
			m_Process.StartInfo.RedirectStandardOutput = true;
		
			m_Process.Exited += binary2textProcess_Exited;

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
	
		private void binary2textProcess_Exited(object sender, EventArgs e)
		{
			m_Process.Close();
			m_Process.Dispose();
			m_Process = null;
			
			// TOOD wait while it exists
			if( File.Exists( m_BinaryFiles[m_FileIndex].FullName + ".txt" ) )
				m_TextFiles[m_FileIndex] = new FileInfo(m_BinaryFiles[m_FileIndex].FullName + ".txt");
			else
				Debug.LogError( "Could not find converted text file for " + m_BinaryFiles[m_FileIndex].Name + " : " + e.ToString() );

			m_FileIndex++;
		}
	}
}
