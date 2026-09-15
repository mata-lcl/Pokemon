using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Pokemon.Application
{
    public sealed class SaveFileRepository
    {
        private readonly string _saveDirectory;

        /// <summary>
        /// 创建使用指定目录的 JSON 存档仓储。
        /// </summary>
        /// <param name="saveDirectory">存放全部存档栏文件的目录。</param>
        public SaveFileRepository(string saveDirectory)
        {
            _saveDirectory = saveDirectory;
        }

        /// <summary>
        /// 将存档数据写入其对应的存档栏文件。
        /// </summary>
        /// <param name="data">需要序列化并写入的数据。</param>
        public void Write(SaveGameData data)
        {
            Directory.CreateDirectory(_saveDirectory);
            File.WriteAllText(
                GetSlotPath(data.slotIndex),
                JsonUtility.ToJson(data, true));
        }

        /// <summary>
        /// 从指定存档栏读取并反序列化存档数据。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        /// <returns>指定存档栏中的存档数据。</returns>
        public SaveGameData Read(int slotIndex)
        {
            return JsonUtility.FromJson<SaveGameData>(
                File.ReadAllText(GetSlotPath(slotIndex)));
        }

        /// <summary>
        /// 返回指定存档栏是否已有存档文件。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        public bool Exists(int slotIndex)
        {
            return File.Exists(GetSlotPath(slotIndex));
        }

        /// <summary>
        /// 返回指定存档栏的摘要信息。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        public SaveSlotSummary GetSlotSummary(int slotIndex)
        {
            if (!Exists(slotIndex))
                return new SaveSlotSummary(
                    slotIndex,
                    false,
                    string.Empty,
                    string.Empty,
                    0);

            SaveGameData data = Read(slotIndex);
            return new SaveSlotSummary(
                slotIndex,
                true,
                data.savedAt,
                data.sceneName,
                data.party != null ? data.party.Count : 0);
        }

        /// <summary>
        /// 返回指定存档栏对应的文件路径。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        private string GetSlotPath(int slotIndex)
        {
            return Path.Combine(_saveDirectory, $"slot_{slotIndex + 1}.json");
        }
    }
}
