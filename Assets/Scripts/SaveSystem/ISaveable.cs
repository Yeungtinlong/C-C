namespace Danny.SaveSystem
{
    public interface ISaveable
    {
        public void Save(Save save);
        public void Load(Save save);
    }
}