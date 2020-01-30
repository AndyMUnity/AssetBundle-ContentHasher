using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace BundleHashing
{
	/// <summary>
	/// 
	/// </summary>
	public class HashReporter
	{
		private string m_OutputFilePath;
		private List<KeyValuePair<string,string>> m_Data = new List<KeyValuePair<string, string>>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="outputFilePath"></param>
		public HashReporter( string outputFilePath )
		{
			m_OutputFilePath = outputFilePath;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="bundle"></param>
		/// <param name="hash"></param>
		public void AddAssetBundle( FileInfo bundle, string hash )
		{
			m_Data.Add( new KeyValuePair<string, string>(bundle.FullName, hash) );
		}

		/// <summary>
		/// 
		/// </summary>
		public void Write()
		{
			StringBuilder json = new StringBuilder("[\n");

			foreach( KeyValuePair<string,string> pair in m_Data )
			{
				json.Append( "{ \"AssetBundle\": \"" );
				json.Append( pair.Key );
				json.Append( "\", \"Hash\": \"" );
				json.Append( pair.Value );
				json.Append( "\", \"CRC\": " );
				uint crc = 0;
				BuildPipeline.GetCRCForAssetBundle( pair.Key, out crc );
				json.Append( crc.ToString() );
				json.Append( " }\n" );
			}
			json.Append( "]" );
			
			File.WriteAllText( m_OutputFilePath, json.ToString() );
		}
	}
}