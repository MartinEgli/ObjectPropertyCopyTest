using System;

namespace ObjectCopyTest
{
    public interface ICopyProvider
    {
        void Copy<T, TU>(T source, TU target);
        Action<TSource, TTarget> CopyAction<TSource, TTarget>();
        Action<T, T> CopyAction<T>();
    }
}