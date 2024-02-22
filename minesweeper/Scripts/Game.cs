using UnityEngine;

public class Game : MonoBehaviour {
    private Board board;

    public int width = 16, height = 16;
    public int minesCount = 32;
    public Cell[,] cells;

    bool gameover;


    /// OnValidate sluzi za ogranicavanje menjanja vrednosti promenljivih u editoru izvan zadatih vrednosti. I slicna ogranicenja.
    private void OnValidate() {
        minesCount = Mathf.Clamp(minesCount, 0, width * height);    /// Clamp znaci da vrednost promenljive mora biti u zadatom opsegu.
    }

    private void Awake() {
        board = GetComponentInChildren<Board>();
    }

    private void Start() {
        NewGame();
        gameover = false;

        Camera.main.transform.position = new Vector3(width / 2, height / 2, -10);
    }

    private void Update() {
        if (!gameover) {
            if (Input.GetMouseButtonDown(1)) Flag();  /// Argument 0 je levi klik, argument 1 je desni klik.
            else if (Input.GetMouseButtonDown(0)) Reveal();
        }
        if (Input.GetKeyDown(KeyCode.R)) NewGame();   /// Nova partija kad se pritisne 'R'.
    }

    public void NewGame() {
        gameover = false;
        cells = new Cell[height, width];

        GenerateCells();
        GenerateMines();
        GenerateNumbers();
        board.DrawBoard(cells);
    }

    private void GenerateCells() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = new Cell();
                cell.position = new Vector3Int(x, y, 0);
                cell.type = Cell.Type.Empty;
                cells[x, y] = cell;
            }
        }
    }
    private void GenerateMines() { 
        for (int i = 0; i < minesCount; i++) {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            /// Za slucaj da smo na tom polju vec postavili minu, trazimo prvo naredno polje pocev od njega koje je slobodno.
            while (cells[x, y].type == Cell.Type.Mine) {
                if (++x >= width) {
                    x = 0;
                    if (++y >= height) y = 0;
                }
            }

            cells[x, y].type = Cell.Type.Mine;
        }
    }
    private void GenerateNumbers() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = cells[x, y];
                if (cell.type == Cell.Type.Mine) continue;

                cell.number = CountSurroundingMines(x, y);
                if (cell.number > 0) cell.type = Cell.Type.Number;
                cells[x, y] = cell;
            }
        }
    }
    private int CountSurroundingMines(int cellX, int cellY) {
        int count = 0;

        for (int adjX = -1; adjX <= 1; adjX++) {
            for (int adjY = -1; adjY <= 1; adjY++) {
                if (adjX == 0 && adjY == 0) continue;           /// Prosledjeno polje ne gledamo, samo okruzujuca.

                int x = cellX + adjX;
                int y = cellY + adjY;

                if (GetCell(x, y).type == Cell.Type.Mine) count++;
            }
        }

        return count;
    }

    private Cell GetCell(int cellX, int cellY) {
        if (!IsValidCell(cellX, cellY)) return new Cell();  /// Pozicija na kojoj se kliknulo nije unutar tabele. Ne moze da se vrati null jer se ocekuje struktura, pa 
                                                            ///     vracamo novonapravljenu celiju ciji ce atribut type imati vrednost invalid.
        return cells[cellX, cellY];
    }
    private bool IsValidCell(int cellX, int cellY) {
        return cellX >= 0 && cellX < width && cellY >= 0 && cellY < height;
    }


    private void Flag() {
        /// Strelica misa se nalazi u Screen prostoru, a tilemap je u WorldSpace prostoru. Pa je potrebna konverzija Screen->WorldSpace, sto se radi preko glavne kamere,
        ///  kako bismo ispitali da li se strelica misa nalazi na nekom polju tabele ili ne.
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(mouseWorldPosition);

        Cell cell = GetCell(cellPosition.x, cellPosition.y);

        if (cell.type == Cell.Type.Invalid || cell.revealed) return;    /// Sadrzaj polja je vec otkriven, ili je kliknuto negde izvan tabele, pa ne treba nista uraditi. 

        cell.flagged = !cell.flagged;
        cells[cellPosition.x, cellPosition.y] = cell;
        board.DrawBoard(cells);
    }
    private void Reveal() {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(mouseWorldPosition);

        Cell cell = GetCell(cellPosition.x, cellPosition.y);

        
        if (cell.type == Cell.Type.Invalid || cell.revealed || cell.flagged) return;    // Ako je oznaceno zastavicom, mora prvo da se skine zastavica da bi levi klik funkcionisao.
        switch (cell.type) {
            case Cell.Type.Empty:
                Flood(cell);
                CheckForWin();
                break;
            case Cell.Type.Mine:
                cell.exploded = true;
                EndGame();
                break;
            case Cell.Type.Number:
                cell.revealed = true;
                cells[cellPosition.x, cellPosition.y] = cell;
                CheckForWin();
                break;
        }

        board.DrawBoard(cells);
    }

    // Rekurzivna f-ja koja se poziva kada se klikne na prazno polje. Otkriva sva okolna prazna polja i brojeve (ali ne i dijagonalno).
    private void Flood(Cell cell) {
        if (cell.revealed || cell.flagged) return;
        if (cell.type != Cell.Type.Empty && cell.type != Cell.Type.Number) return;

        cell.revealed = true;
        cells[cell.position.x, cell.position.y] = cell;
        if (cell.type == Cell.Type.Number) return;              /// Kad stignemo do broja, ne otkrivamo njegova okolna polja, nego sa njime prestajemo.
        
        Flood(GetCell(cell.position.x - 1, cell.position.y));
        Flood(GetCell(cell.position.x, cell.position.y - 1));
        Flood(GetCell(cell.position.x + 1, cell.position.y));
        Flood(GetCell(cell.position.x, cell.position.y + 1));
    }

    private void CheckForWin() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (!cells[x, y].revealed && cells[x, y].type != Cell.Type.Mine) return;
            }
        }

        gameover = true;
        FlagAllMines();
        Debug.Log("Pobeda!");
    }
    private void EndGame() {
        RevealAllMines();
        gameover = true;
        Debug.Log("Poraz!");
    }
    private void FlagAllMines() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (cells[x, y].type == Cell.Type.Mine) cells[x, y].flagged = true;
            }
        }
        //board.DrawBoard(cells);
    }
    private void RevealAllMines() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (cells[x, y].type == Cell.Type.Mine) cells[x, y].revealed = true;
            }
        }
        //board.DrawBoard(cells);
    }
}
