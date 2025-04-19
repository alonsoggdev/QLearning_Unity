using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using UnityEngine;

public class Matrix
{
    Cell[,] m_cells;

    private char[,] m_board;
    int[] m_start;
    int[] m_goal;
    private int m_rows;
    private int m_columns;

    string dataFolder;
    string matrixFile;
    string cellsJsonFile;
    string savedMatricesFile;

    public int[] get_start()
    {
        return m_start;
    }

    public int[] get_goal()
    {
        return m_goal;
    }

    public Matrix()
    {
        dataFolder = Path.Combine(Application.dataPath, "Data");
        matrixFile = Path.Combine(dataFolder, "Matrix.txt");
        cellsJsonFile = Path.Combine(dataFolder, "Celdas.json");
        savedMatricesFile = Path.Combine(dataFolder, "Matrix_Save.txt");

        if (!Directory.Exists(dataFolder))
        {
            Directory.CreateDirectory(dataFolder);
        }

        delete_file(savedMatricesFile);
        create_file(savedMatricesFile);


        m_board = create_board(matrixFile);
        if (m_board == null)
        {
            Debug.LogError("La matriz no es válida. Debe contener exactamente una casilla de salida y una de entrada.");
            return;
        }

        m_cells = create_cells(m_board);

        write_cells_json(m_cells);

        render_grid(m_board);
    }

    void render_grid(char[,] board)
    {
        GameObject gridRendererGO = GameObject.Find("GridRenderer");
        gridRendererGO.GetComponent<GridRenderer>().RenderGrid(board);
    }

    void write_cells_json(Cell[,] cell_grid)
    {
        create_file(cellsJsonFile);
        var serializable_cells = cell_grid.Cast<Cell>().ToList();
        string json = JsonConvert.SerializeObject(serializable_cells, Formatting.Indented);
        File.WriteAllText(cellsJsonFile, json);
    }

    Cell[,] create_cells(char[,] board)
    {
        Cell[,] grid = new Cell[m_rows, m_columns];

        for (int i = 0; i < m_rows; i++)
        {
            for (int j = 0; j < m_columns; j++)
            {
                char type = board[i, j];
                grid[i, j] = new Cell
                {
                    Type = type,
                    Row = i,
                    Column = j,
                    Up = i > 0 && board[i - 1, j] == '0',
                    Down = i < m_rows - 1 && board[i + 1, j] == '0',
                    Left = j > 0 && board[i, j - 1] == '0',
                    Right = j < m_columns - 1 && board[i, j + 1] == '0'
                };
            }
        }

        return grid;
    }

    char[,] create_board(string file_path)
    {
        if (!File.Exists(file_path))
        {
            Debug.LogError($"Archivo de matriz no encontrado en: {file_path}");
            return null;
        }

        int startCount = 0;
        int goalCount = 0;

        string[] lines = File.ReadAllLines(file_path);

        m_rows = lines.Length;
        m_columns = lines[0].Split(' ').Length;

        char[,] board = new char[m_rows, m_columns];

        for (int i = 0; i < m_rows; i++)
        {
            string[] numbers = lines[i].Split(' ');

            for (int j = 0; j < m_columns; j++)
            {
                board[i, j] = char.Parse(numbers[j]);

                if(board[i, j] == 'X')
                {
                    startCount++;
                    m_start = new int[2] { i, j };
                }
                else if (board[i, j] == 'S')
                {
                    goalCount++;
                    m_goal = new int[2] { i, j };
                }
            }
        }

        if (startCount != 1 || goalCount != 1)
        {
            Debug.LogError("La matriz no es válida. Debe contener exactamente una casilla de salida y una de entrada.");
            return null;
        }

        return board;
    }

    public void reset_to_initial_position(int[] current_position, int current_episode, int step)
    {
        // Clear previous X position
        for (int i = 0; i < m_rows; i++)
        {
            for (int j = 0; j < m_columns; j++)
            {
                if (m_board[i, j] == 'X' && (i != current_position[0] || j != current_position[1]))
                {
                    this.set_board_value(i, j, '0');
                }
            }
        }

        // Set X at the initial position
        this.set_board_value(current_position[0], current_position[1], 'X');

    }

    public void save_board(int episode, int step) 
    {
        string data_folder = Path.Combine(Application.dataPath, "Data");
        string path = Path.Combine(data_folder, "Matrix_Save.txt");

        Debug.Log("Ruta absoluta: " + Path.GetFullPath(path));
        try
        {
            using (StreamWriter writer = new StreamWriter(path, append: true))
            {
                writer.WriteLine("----- Episodio "+ episode + ", Movimento " + step + " -----");
                for (int i = 0; i < m_rows; i++)
                {
                    for (int j = 0; j < m_columns; j++)
                    {
                        writer.Write(m_board[i, j] + " ");
                    }
                    writer.WriteLine();
                }
            }
            Debug.Log("Matriz guardada correctamente.");
        }
        catch (Exception ex)
        {
            Debug.Log("Error al guardar la matriz: " + ex.Message);
        }
    }

    public char[,] get_board()
    {
        return m_board;
    }

    public int get_rows()
    {
        return m_rows;
    }

    public int get_columns()
    {
        return m_columns;
    }

    public void set_board_value(int row, int col, char value)
    {
        m_board[row, col] = value;
    }

    void delete_file(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    void create_file(string path)
    {
        File.Create(path).Close();
    }

    public void find_start_and_goal_positions(char[,] board)
    {
        int[] start = new int[2] { -1, -1};
        int[] goal = new int[2] { -1, -1};

        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                if (board[i, j] == 'X')
                {
                    start = new int[2] { i, j };
                }
                else if (board[i, j] == 'S')
                {
                    goal = new int[2] { i, j };
                }
            }
        }

        m_start = start;
        m_goal = goal;
    }
}
