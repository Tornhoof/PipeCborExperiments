namespace StreamingCbor
{
    public enum CborType : byte
    {
        PositiveInteger = 0,
        NegativeInteger = 1,
        ByteString = 2,
        TextString = 3,
        Array = 4,
        Map = 5,
        Tag = 6,
        Primitive = 7
    }
}