﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PTGame.Framework;
using System.Text;

namespace PTGame.Framework
{

    public class AssetDataTable : TSingleton<AssetDataTable>
    {
        [Serializable]
        public class SerializeData
        {
            private AssetDataGroup.SerializeData[] m_AssetDataGroup;

            public AssetDataGroup.SerializeData[] assetDataGroup
            {
                get { return m_AssetDataGroup; }
                set { m_AssetDataGroup = value; }
            }
        }

        private List<AssetDataGroup> m_ActiveAssetDataGroup = new List<AssetDataGroup>();
        private List<AssetDataGroup> m_AllAssetDataGroup = new List<AssetDataGroup>();

        public void SwitchLanguage(string key)
        {
            m_ActiveAssetDataGroup.Clear();

            string languageKey = string.Format("[{0}]", key);

            for (int i = m_AllAssetDataGroup.Count - 1; i >= 0; --i)
            {
                AssetDataGroup group = m_AllAssetDataGroup[i];

                if (!group.key.Contains("i18res"))
                {
                    m_ActiveAssetDataGroup.Add(group);
                }
                else if (group.key.Contains(languageKey))
                {
                    m_ActiveAssetDataGroup.Add(group);
                }

            }
            Log.i("AssetDataTable Switch 2 Language:" + key);
        }

        public void Reset()
        {
            for (int i = m_AllAssetDataGroup.Count - 1; i >= 0; --i)
            {
                m_AllAssetDataGroup[i].Reset();
            }

            m_AllAssetDataGroup.Clear();
            m_ActiveAssetDataGroup.Clear();
        }

        public int AddAssetBundleName(string name, string[] depends, string md5, int fileSize, out AssetDataGroup group)
        {
            group = null;

            if (string.IsNullOrEmpty(name))
            {
                return -1;
            }

            string key = null;

            key = GetKeyFromABName(name);

            if (key == null)
            {
                return -1;
            }

            group = GetAssetDataGroup(key);

            if (group == null)
            {
                group = new AssetDataGroup(key);
                Log.i("#Create Config Group:" + key);
                m_AllAssetDataGroup.Add(group);
            }

            return group.AddAssetBundleName(name, depends, md5, fileSize);
        }

        public string GetAssetBundleName(string assetName, int index)
        {
            string result = null;
            for (int i = m_ActiveAssetDataGroup.Count - 1; i >= 0; --i)
            {
                if (!m_ActiveAssetDataGroup[i].GetAssetBundleName(assetName, index, out result))
                {
                    continue;
                }

                return result;
            }
            Log.w(string.Format("Failed GetAssetBundleName : {0} - Index:{1}", assetName, index));
            return null;
        }

        public List<ABUnit> GetAllABUnit()
        {
            List<ABUnit> result = new List<ABUnit>();
            for (int i = m_AllAssetDataGroup.Count - 1; i >= 0; --i)
            {
                result.AddRange(m_AllAssetDataGroup[i].GetAllABUnit());
            }
            return result;
        }

        public ABUnit GetABUnit(string name)
        {
            ABUnit result = null;

            for (int i = m_AllAssetDataGroup.Count - 1; i >= 0; --i)
            {
                result = m_AllAssetDataGroup[i].GetABUnit(name);
                if (result != null)
                {
                    break;
                }
            }

            return result;
        }

        //该函数的使用对打包规划要求太高，暂不提供
        public string GetAssetBundlePath(string assetName)
        {
            string result = null;
            for (int i = m_ActiveAssetDataGroup.Count - 1; i >= 0; --i)
            {
                if (!m_ActiveAssetDataGroup[i].GetAssetBundlePath(assetName, out result))
                {
                    continue;
                }

                return result;
            }
            Log.w(string.Format("Failed GetAssetBundlePath : {0}", assetName));
            return null;
        }

        public string[] GetAllDependenciesByUrl(string url)
        {
            string abName = ProjectPathConfig.AssetBundleUrl2Name(url);
            string[] depends = null;

            for (int i = m_ActiveAssetDataGroup.Count - 1; i >= 0; --i)
            {
                if (!m_ActiveAssetDataGroup[i].GetAssetBundleDepends(abName, out depends))
                {
                    continue;
                }

                return depends;
            }

            return null;
        }

        public AssetData GetAssetData(string assetName)
        {
            for (int i = m_ActiveAssetDataGroup.Count - 1; i >= 0; --i)
            {
                AssetData result = m_ActiveAssetDataGroup[i].GetAssetData(assetName);
                if (result == null)
                {
                    continue;
                }
                return result;
            }
            //Log.w(string.Format("Not Find Asset : {0}", assetName));
            return null;
        }

        public bool AddAssetData(string key, AssetData data)
        {
            var group = GetAssetDataGroup(key);
            if (group == null)
            {
                Log.e("Not Find Group:" + key);
                return false;
            }
            return group.AddAssetData(data);
        }

        public void LoadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            object data = SerializeHelper.DeserializeBinary(path);

            if (data == null)
            {
                Log.e("Failed Deserialize AssetDataTable:" + path);
                return;
            }

            SerializeData sd = data as SerializeData;

            if (sd == null)
            {
                Log.e("Failed Load AssetDataTable:" + path);
                return;
            }

            //Log.i("Load AssetConfig From File:" + path);

            string parentFolder = PathHelper.GetFolderPath(path);

            SetSerizlizeData(sd, parentFolder);
        }

        public void Save(string outPath)
        {
            SerializeData sd = new SerializeData();

            sd.assetDataGroup = new AssetDataGroup.SerializeData[m_AllAssetDataGroup.Count];

            for (int i = 0; i < m_AllAssetDataGroup.Count; ++i)
            {
                sd.assetDataGroup[i] = m_AllAssetDataGroup[i].GetSerializeData();
            }

            if (SerializeHelper.SerializeBinary(outPath, sd))
            {
                Log.i("Success Save AssetDataTable:" + outPath);
            }
            else
            {
                Log.e("Failed Save AssetDataTable:" + outPath);
            }
        }

        public void Dump()
        {
            //StringBuilder builder = new StringBuilder();

            Log.i("#DUMP AssetDataTable BEGIN");

            for (int i = 0; i < m_AllAssetDataGroup.Count; ++i)
            {
                m_AllAssetDataGroup[i].Dump();
            }

            Log.i("#DUMP AssetDataTable END");
        }

        private void SetSerizlizeData(SerializeData data, string path)
        {
            if (data == null || data.assetDataGroup == null)
            {
                return;
            }

            for (int i = data.assetDataGroup.Length - 1; i >= 0; --i)
            {
                m_AllAssetDataGroup.Add(BuildAssetDataGroup(data.assetDataGroup[i], path));
            }
        }

        private AssetDataGroup BuildAssetDataGroup(AssetDataGroup.SerializeData data, string path)
        {
            return new AssetDataGroup(data, path);
        }

        private AssetDataGroup GetAssetDataGroup(string key)
        {
            for (int i = m_AllAssetDataGroup.Count - 1; i >= 0; --i)
            {
                if (m_AllAssetDataGroup[i].key.Equals(key))
                {
                    return m_AllAssetDataGroup[i];
                }
            }

            return null;
        }

        private string GetKeyFromABName(string name)
        {
            int pIndex = name.IndexOf('/');

            if (pIndex < 0)
            {
                return name;
            }

            string key = name.Substring(0, pIndex);

            if (name.Contains("i18res"))
            {
                int i18Start = name.IndexOf("i18res") + 7;
                name = name.Substring(i18Start);
                pIndex = name.IndexOf('/');
                if (pIndex < 0)
                {
                    Log.w("Not Valid AB Path:" + name);
                    return null;
                }

                string language = string.Format("[{0}]", name.Substring(0, pIndex));
                key = string.Format("{0}-i18res-{1}", key, language);
            }

            return key;
        }

    }
}
