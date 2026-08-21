using System;
using System.Collections.Generic;

namespace Pokemon.Domain
{
    [Serializable]
    public class QuestRuntimeData
    {
        public string questId;
        public QuestState state;
        public List<int> objectiveProgress = new List<int>();
    }
}
