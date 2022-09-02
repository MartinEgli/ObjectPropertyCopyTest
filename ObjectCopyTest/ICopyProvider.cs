using System;

namespace ObjectCopyTest
{
    public interface ICopyProvider
    {
        void Copy<TSource, TTarget>(TSource source, TTarget target);
        Action<TSource, TTarget> CopyAction<TSource, TTarget>();
        Action<T, T> CopyAction<T>();
    }
}