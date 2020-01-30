using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using UnityEngine;

namespace BundleHashing
{
    /// <summary>
    /// 
    /// </summary>
    public class LegacyBundleHashing
    {
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
        /// <param name="buildPath"></param>
        /// <param name="manifest"></param>
        /// <param name="outputPath"></param>
        /// <param name="hashingMethod"></param>
        /// <returns></returns>
        public static LegacyBundleHashing GenerateAssetBundleHashes( string buildPath, AssetBundleManifest manifest, string outputPath, IBundleHashingMethod hashingMethod = null )
        {
            string[] bundles = manifest.GetAllAssetBundles();
            string[] bundlesPaths = new string[bundles.Length];
            for( int i = 0; i < bundles.Length; ++i )
                bundlesPaths[i] = Path.Combine( buildPath, bundles[i] );

            return GenerateAssetBundleHashes( bundlesPaths, outputPath, hashingMethod );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bundlePaths"></param>
        /// <param name="outputPath"></param>
        /// <param name="hashingMethod"></param>
        /// <returns></returns>
        public static LegacyBundleHashing GenerateAssetBundleHashes( IList<string> bundlePaths, string outputPath, IBundleHashingMethod hashingMethod = null )
        {
            List<FileInfo> files = new List<FileInfo>(bundlePaths.Count);
            foreach( string path in bundlePaths )
            {
                if( File.Exists( path ) )
                    files.Add( new FileInfo( path ) );
                else
                    Debug.LogError( "Could not find AssetBundle at " + path );
            }
            return GenerateAssetBundleHashes( files, outputPath, hashingMethod );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bundleFiles"></param>
        /// <param name="outputPath"></param>
        /// <param name="hashingMethod"></param>
        /// <returns></returns>
        public static LegacyBundleHashing GenerateAssetBundleHashes( IList<FileInfo> bundleFiles, string outputPath, IBundleHashingMethod hashingMethod = null )
        {
            return new LegacyBundleHashing( bundleFiles, outputPath, hashingMethod );
        }

        private IList<FileInfo> m_AssetBundles;
        private AssetBundleUnpacker m_Unpacker;
        private Binary2TextProcessor m_2texter;
        private IBundleHashingMethod m_Hasher;
        private HashReporter m_Reporter;
        
        private LegacyBundleHashing( IList<FileInfo> bundlePaths, string outputPath, IBundleHashingMethod hashingMethod = null )
        {
            m_Hasher = hashingMethod != null ? hashingMethod : new MD5Hasher();
            m_Reporter = new HashReporter( outputPath );
            m_AssetBundles = bundlePaths;
            m_Unpacker = new AssetBundleUnpacker( bundlePaths );
            m_Unpacker.OnCompleted = OnUnpacked;
            m_Unpacker.Start();
        }

        private void OnUnpacked()
        {
            // Now we need to convert any .sharedresource files or no extention files to text
            // this will eliminate any unwanted header information being included in the hashing 
            
            List<DirectoryInfo> dirs = m_Unpacker.UnpackedDirectories;
            List<FileInfo> filesToConvert = new List<FileInfo>(dirs.Count * 2);
            foreach( DirectoryInfo dir in dirs )
                GetFilesForConverting( dir, filesToConvert );

            m_2texter = new Binary2TextProcessor( filesToConvert );
            m_2texter.OnCompleted = CalculateHashes;
            m_2texter.Start();
        }

        private void CalculateHashes()
        {
            List<DirectoryInfo> unpackedDirectories = m_Unpacker.UnpackedDirectories;
            if( m_AssetBundles.Count != unpackedDirectories.Count )
            {
                Debug.LogError( "Unknown issue, extracted output != assetBundle input" );
                return;
            }
            
            for( int i = 0; i < unpackedDirectories.Count; ++i )
            {
                IEnumerable<FileInfo> files = unpackedDirectories[i].EnumerateFiles("*", SearchOption.AllDirectories);
                foreach( FileInfo file in files )
                {
                    if( file.Extension == ".resS" || file.Extension == ".resource" || file.Extension == ".txt" )
                    {
                        m_Hasher.Feed( file );
                    }
                }

                string hash = m_Hasher.Complete();
                Debug.Log( "hash for " + m_AssetBundles[i].Name + " is " + hash );
                m_Reporter.AddAssetBundle( m_AssetBundles[i], hash );
            }
            
            m_Reporter.Write();
            Cleanup();
            OnCompleted?.Invoke();
        }
        
        private void Cleanup()
        {
            List<DirectoryInfo> unpackedDirectories = m_Unpacker.UnpackedDirectories;
            foreach( DirectoryInfo unpackedDirectory in unpackedDirectories )
            {
                if( unpackedDirectory.Exists )
                    unpackedDirectory.Delete(true);
            }
            
            m_Hasher.Dispose(); // Only if not user provided?
            m_AssetBundles = null;
            m_Unpacker = null;
            m_2texter = null;
        }

        private static void GetFilesForConverting( DirectoryInfo d, List<FileInfo> files )
        {
            foreach( FileInfo file in d.EnumerateFiles() )
            {
                if( File.Exists( file.FullName ) == false )
                {
                    Debug.LogError( "How did this happen?" );
                    continue;
                }
                if( file.Extension == ".sharedAssets" || file.Extension == "" )
                    files.Add( file );
            }

            // expect this to be never needed.. \(o.o)/
            foreach( DirectoryInfo directory in d.EnumerateDirectories() )
            {
                if( directory.FullName != d.FullName )
                    GetFilesForConverting( directory, files );
            }
        }
    }
}
