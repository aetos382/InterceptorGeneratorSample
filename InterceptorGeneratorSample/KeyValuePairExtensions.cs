using System.Collections.Generic;

namespace InterceptorGeneratorSample;

internal static class KeyValuePairExtensions
{
    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> instance,
        out TKey key,
        out TValue value)
    {
        key = instance.Key;
        value = instance.Value;
    }
}