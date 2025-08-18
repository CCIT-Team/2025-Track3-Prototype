
public interface IPoolable
{
    public void SetPoolInstance(GameObjectPool poolInstance);
    public bool ComparePoolInstance(GameObjectPool poolInstance);
    public bool IsPooled();

    public void SetPooled(bool option);
}