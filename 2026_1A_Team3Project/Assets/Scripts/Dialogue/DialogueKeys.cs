namespace Team3Project.Dialogue
{
    public static class DialogueKeys
    {
        public static string ChapterEntry(int chapter)
        {
            return $"Dialogues/Chapter{chapter}_Entry";
        }

        public static string StageIntro(int chapter, int stage)
        {
            return $"Dialogues/Chapter{chapter}_Stage{stage}";
        }
    }
}
