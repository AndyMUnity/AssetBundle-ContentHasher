using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BundleHashing
{
	public interface IBundleHashingMethod : IDisposable
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="file"></param>
		void Feed( FileInfo file );
		
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		string Complete();
	}
}
