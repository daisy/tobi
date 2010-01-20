public class Token
{
	public TokenType Type { get; set;}
    public string Value { get; set; }

	public Token(TokenType aType)
	{
		Type = aType;
		Value = null;
	}

	public Token(TokenType aType, string aValue)
	{
		Type = aType;
		Value = aValue;
	}
}
