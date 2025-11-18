// Fryer tool models
public record FryStandardRequest(string Portion, int Duration);
public record FrySweetPotatoRequest(string Portion, int Duration);
public record FryWaffleRequest(string Portion, int Duration);
public record FryOnionRingsRequest(string Portion, int Duration);
public record AddSaltRequest(bool addSalt);
