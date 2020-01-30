using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BundleHashing
{
	public class MD5Hasher : IBundleHashingMethod
	{
		private MD5 m_MD5;
		private MD5 Md5
		{
			get
			{
				if( m_MD5 == null )
					m_MD5 = MD5.Create();
				return m_MD5;
			}
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="file"></param>
		public void Feed( FileInfo file )
		{
			using( var stream = file.OpenRead( ) )
			{
				byte[] buffer = new byte[4096];
				int read = 0;
				while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
				{
					Md5.TransformBlock(buffer, 0, read, buffer, 0);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string Complete()
		{
			byte[] data = new byte[0];
			Md5.TransformFinalBlock(data, 0, 0);
			data = Md5.Hash;
			StringBuilder final = new StringBuilder(32);
			for (int i = 0; i < data.Length; i++)
				final.Append(data[i].ToString("x2"));

			m_MD5?.Initialize();
			return final.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			m_MD5?.Dispose();
		}
	}
}