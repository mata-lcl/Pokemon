using System;
using System.Collections.Generic;
using System.IO;
using Pokemon.Domain;
using UnityEngine;

namespace Pokemon.Application
{
    public static class SaveGameService
    {
        public const int SlotCount = 3;

        private static readonly SaveGameMapper Mapper = new SaveGameMapper();

        /// <summary>
        /// 将当前玩家位置、队伍、仓库和背包信息写入指定本地存档栏。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        /// <param name="sceneName">保存时玩家所在的场景名称。</param>
        /// <param name="playerPosition">保存时玩家所在位置。</param>
        public static void Save(
            int slotIndex,
            string sceneName,
            Vector3 playerPosition)
        {
            ValidateSlotIndex(slotIndex);
            SaveGameData data = Mapper.CreateSaveData(
                slotIndex,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                sceneName,
                playerPosition);
            CreateRepository().Write(data);
        }

        /// <summary>
        /// 返回全部存档栏的摘要信息。
        /// </summary>
        public static IReadOnlyList<SaveSlotSummary> GetSlotSummaries()
        {
            var repository = CreateRepository();
            List<SaveSlotSummary> summaries = new List<SaveSlotSummary>(SlotCount);
            for (int i = 0; i < SlotCount; i++)
                summaries.Add(repository.GetSlotSummary(i));
            return summaries;
        }

        /// <summary>
        /// 返回指定存档栏是否已有本地存档。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        public static bool HasSave(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return CreateRepository().Exists(slotIndex);
        }

        /// <summary>
        /// 读取指定存档栏并恢复队伍、仓库、出战精灵和背包数据。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        /// <param name="assetCatalog">用于解析存档资源标识的资源目录。</param>
        public static SaveGameData Load(int slotIndex, SaveAssetCatalog assetCatalog)
        {
            ValidateSlotIndex(slotIndex);
            SaveGameData data = CreateRepository().Read(slotIndex);
            Mapper.RestoreState(data, assetCatalog);
            LoadedGameContext.SetPendingPlayerPosition(
                data.sceneName,
                data.playerPosition);
            return data;
        }

        /// <summary>
        /// 返回并清除当前场景等待应用的读档玩家位置。
        /// </summary>
        /// <param name="sceneName">当前加载完成的场景名称。</param>
        /// <param name="playerPosition">返回存档中的玩家世界坐标。</param>
        public static bool TryConsumePlayerPosition(
            string sceneName,
            out Vector3 playerPosition)
        {
            return LoadedGameContext.TryConsumePlayerPosition(
                sceneName,
                out playerPosition);
        }

        /// <summary>
        /// 清除尚未应用的读档玩家位置，用于开始新游戏。
        /// </summary>
        public static void ClearPendingPlayerPosition()
        {
            LoadedGameContext.ClearPendingPlayerPosition();
        }

        /// <summary>
        /// 创建使用玩家本地存档目录的文件仓储。
        /// </summary>
        /// <returns>指向当前玩家存档目录的文件仓储。</returns>
        private static SaveFileRepository CreateRepository()
        {
            return new SaveFileRepository(Path.Combine(
                UnityEngine.Application.persistentDataPath,
                "Saves"));
        }

        /// <summary>
        /// 检查存档栏索引是否在有效范围内。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        private static void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }
    }
}
