using System;
using System.Collections.Generic;

namespace MusicMachine
{
public struct Pair<T1, T2> : IEquatable<Pair<T1, T2>>
{
    public T1 First;
    public T2 Second;

    public Pair(T1 first, T2 second)
    {
        First  = first;
        Second = second;
    }

    public bool Equals(Pair<T1, T2> other) =>
        EqualityComparer<T1>.Default.Equals(First, other.First)
     && EqualityComparer<T2>.Default.Equals(Second, other.Second);

    public static bool operator ==(Pair<T1, T2> left, Pair<T1, T2> right) => left.Equals(right);

    public static bool operator !=(Pair<T1, T2> left, Pair<T1, T2> right) => !left.Equals(right);

    public override bool Equals(object obj) => obj is Pair<T1, T2> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (EqualityComparer<T1>.Default.GetHashCode(First) * 397)
                 ^ EqualityComparer<T2>.Default.GetHashCode(Second);
        }
    }
}
}