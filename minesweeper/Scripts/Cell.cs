using UnityEngine;

public struct Cell {
    public enum Type {              /// Ovo su tri moguca sadrzaja polja. Zastavica, eksplodirano i neotkriveno polje su samo trenutne oznake (slike) za polje a ne sadrzaj.
        Invalid,    /// Kako je ovo prvo polje strukture, kad se napravi nova celija, njen atribut type ce imati vrednost Invalid.
        Number,
        Empty,
        Mine
    };

    public Type type;
    public Vector3Int position;     /// Nije moglo Vector2Int da se korisiti jer Tilemap ocekuje promenljive tipa Vector3Int.
    public int number;              /// U slucaju da celija sadrzi broj, ovde je podatak koji je od 8 brojeva u pitanju.
    public bool flagged;
    public bool exploded;
    public bool revealed;
}
