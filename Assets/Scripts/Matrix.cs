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

    void clear_x_position(int[] current_position)
    {
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
    }

    int[] get_random_empty_cell()
    {
        bool valid = false;

        int row = -1;
        int col = -1;

        while (valid == false)
        {
            row = UnityEngine.Random.Range(0, m_rows);
            col = UnityEngine.Random.Range(0, m_columns);

            if (m_board[row, col] == '0')
            {
                valid = true;
            }
        }

        return new int[] { row, col };
    }

    public void reset_to_initial_position(int[] current_position, int current_episode, int step)
    {
        clear_x_position(current_position);

        // Set X at the initial position
        this.set_board_value(current_position[0], current_position[1], 'X');
        this.save_board(current_episode, step);
    }

    public int[] reset_to_new_random_position(int[] current_position, int current_episode, int step)
    {
        clear_x_position(current_position);

        int[] new_position = get_random_empty_cell();

        // Debug.Log($"Nueva posición aleatoria: {new_position[0]}, {new_position[1]}");

        // Set X at the initial position
        this.set_board_value(new_position[0], new_position[1], 'X');
        this.save_board(current_episode, step);

        return new_position;
    }



    public void reset_to_starting_cell(int[] current_position, int current_episode, int step)
    {
        clear_x_position(current_position);

        // Set X at the starting position
        this.set_board_value(m_start[0], m_start[1], 'X');
        this.save_board(current_episode, step);
    }

    public void save_board(int episode, int step) 
    {
        string data_folder = Path.Combine(Application.dataPath, "Data");
        string path = Path.Combine(data_folder, "Matrix_Save.txt");

        // Debug.Log("Ruta absoluta: " + Path.GetFullPath(path));
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
            // Debug.Log("Matriz guardada correctamente.");
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

    public void print_board()
    {
        Debug.Log("------------------------------");
        Debug.Log("Has creado la sieguiente matriz");
        for (int i = 0; i < m_rows; i++)
        {
            for (int j = 0; j < m_columns; j++)
            {
                Debug.Log(m_board[i, j] + " ");
            }
            Debug.Log("");
        }
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

    public void mark_cell_as_visited(int row, int col)
    {
        mark_cell(row, col, 2);
    }

    public void mark_cell_as_checked(int row, int col)
    {
        mark_cell(row, col, 1);
    }

    public void mark_cell_as_default(int row, int col)
    {
        mark_cell(row, col, 0);
    }

    public void reset_cells_color()
    {
        for (int i = 0; i < m_rows; i++)
        {
            for (int j = 0; j < m_columns; j++)
            {
                GameObject cell = GameObject.Find($"{i}-{j}");
                if (cell == null)
                {
                    Debug.LogError($"No se encontró la celda en la posición ({i}, {j})");
                    continue;
                }
                if (cell.tag != "1")
                {
                    mark_cell_as_default(i, j);
                }
            }
        }
    }

    public void mark_cell(int row, int col, int type)
    {
        GameObject cell = GameObject.Find($"{row}-{col}");

        if (cell == null)
        {
            Debug.LogError($"No se encontró la celda en la posición ({row}, {col})");
            return;
        }

        if (row >= 0 && row < m_rows && col >= 0 && col < m_columns)
        {
            SpriteRenderer spriteRenderer = cell.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                switch (type)
                {
                    case 0:
                        spriteRenderer.color = Color.white;
                        break;
                    case 1:
                        spriteRenderer.color = new Color(0.6f, 0.8f, 1f);
                        break;
                    case 2:
                        spriteRenderer.color = new Color(0.4f, 1f, 0.6f);
                        break;
                    default:
                        Debug.LogError($"Tipo de celda no válido: {type}");
                        break;
                }
            }
            else
            {
                Debug.LogError($"No se encontró el componente SpriteRenderer en la celda ({row}, {col})");
            }
        }
        else
        {
            Debug.LogError($"Índices fuera de rango: ({row}, {col})");
        }
    }
}
