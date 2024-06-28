using System.Runtime.CompilerServices;

namespace Hydrogen;

public class Map<T1, T2>
{
    public List<T1> Keys;
    public List<T2> Values;

    public Map()
    {
        Keys = [];
        Values = [];
    }

    public void Add(T1 key, T2 value)
    {
        Keys.Add(key);
        Values.Add(value);
    }

    public void Remove(T1 key)
    {
        int index = GetIndexOfKey(key);

        Keys.RemoveAt(index);
        Values.RemoveAt(index);
    }

    public void PopBack()
    {
        Keys.RemoveAt(Keys.Count - 1);
        Values.RemoveAt(Values.Count - 1);
    }

    public void PopBack(int count)
    {
        for (int i = 0; i < count; i++)
            PopBack();
    }

    public T2 GetValueByKey(T1 key) => Values[GetIndexOfKey(key)];
    public T2 GetValueByIndex(int index) => Values[index];

    public bool ContainsKey(T1 key) => Keys.Contains(key);

    public int Count => Keys.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetIndexOfKey(T1 key)
    {
        return Keys.FindIndex(x => EqualityComparer<T1>.Default.Equals(x, key));
    }
}