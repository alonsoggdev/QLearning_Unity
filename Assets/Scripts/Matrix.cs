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
    private int m_rows;
    private int m_columns;

    string dataFolder;
    string matrixFile;
    string cellsJsonFile;
    string savedMatricesFile;

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
            }
        }

        return board;
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
}
