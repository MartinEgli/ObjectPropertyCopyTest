namespace ObjectCopyTest
{

    public static class CopyDirect
    {
        public static void Copy(this ITestObject source, ITestObject target)
        {
            target.Number1 = source.Number1;
            target.Number2 = source.Number2;
            target.Number3 = source.Number3;
            target.Number4 = source.Number4;
            target.Number5 = source.Number5;
            target.Text1 = source.Text1;
            target.Text2 = source.Text2;
            target.Text3 = source.Text3;
            target.Text4 = source.Text4;
            target.Text5 = source.Text5;
        }
    }
}