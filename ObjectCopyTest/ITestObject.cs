namespace ObjectCopyTest
{
    public interface ITestObject
    {
        [Copyable] string Text1 { get; set; }
        [Copyable] string Text2 { get; set; }
        [Copyable] string Text3 { get; set; }
        [Copyable] string Text4 { get; set; }
        [Copyable] string Text5 { get; set; }

        [Copyable] double Number1 { get; set; }
        [Copyable] double Number2 { get; set; }
        [Copyable] double Number3 { get; set; }
        [Copyable] double Number4 { get; set; }
        [Copyable] double Number5 { get; set; }
    }
}