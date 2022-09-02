namespace ObjectCopyTest
{
    public sealed class Copy_ObjectCopyTest_ITestObject_ObjectCopyTest_ITestObject
    {
        public static void Copy(object source, object target)
        {
            var t = (ITestObject)target;
            var s = (ITestObject)source;
            t.Text1 = s.Text1;
            t.Text2 = s.Text2;
            t.Text3 = s.Text3;
            t.Text4 = s.Text4;
            t.Text5 = s.Text5;
            t.Number1 = s.Number1;
            t.Number2 = s.Number2;
            t.Number3 = s.Number3;
            t.Number4 = s.Number4;
            t.Number5 = s.Number5;
        }

        public static System.Action<object, object> CopyAction => Copy;
    }
}