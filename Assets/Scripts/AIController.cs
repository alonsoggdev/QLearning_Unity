using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using UnityEngine;


using DebugLog = UnityEngine.Debug;

public class AIController : MonoBehaviour
{
    [Header("AI Parameters")]
    [SerializeField] bool    avoidLoops = true; //? Set to true to avoid infinite loops
    [SerializeField] float   stepDelay = 0.0001f; //? Delay between steps in seconds

    private System.Random rand = new System.Random();

    Matrix      m_matrix;
    int         m_rows;
    int         m_columns;
    float[,]    qTable;

    int[] start_position;
    int[] goal_position;
    int[] current_position;

    CanvasManager canvasManager;

    GameManager.Algorithm   algorithm;
    private float           learning_rate;
    private float           discount_factor;
    private float           goal_award;
    private float           movement_award = -1f;
    private float           gift_award;
    Coroutine               algorithmCoroutine;

    public void set_algorithm(int algorithm)
    {
        this.algorithm = (GameManager.Algorithm)algorithm;
    }

    public void set_learning_rate(float learning_rate)
    {
        this.learning_rate = learning_rate;
    }

    public void set_discount_factor(float discount_factor)
    {
        this.discount_factor = discount_factor;
    }

    public void set_goal_award(float goal_award)
    {
        this.goal_award = goal_award;
    }

    public void set_movement_award(float movement_award)
    {
        this.movement_award = movement_award;
    }

    public void set_gift_award(float gift_award)
    {
        this.gift_award = gift_award;
    }


    [Header("SARSA Parameters")]
    [SerializeField]int episodes    = 10000;
    [SerializeField]float epsilon   = 0.99f;

    // Control parameters for saving
    bool save_all_movements = false;
    int save_frequency      = 1;

    // Track when the first successful path is found
    List<int> success_episodes = new List<int>();
    List<int> success_steps = new List<int>();

    int[] previous_position = new int[2] { -1, -1 };
    
    List<int> visited_states = new List<int>();
    int cycle_check_limit = 4;

    enum Action
    {
        Up,
        Down,
        Left,
        Right
    }

    void Awake()
    {
        canvasManager = FindObjectOfType<CanvasManager>();
        if (canvasManager == null)
        {
            DebugLog.LogError("CanvasManager not found in the scene.");
        }
    }

    void Start()
    {
        m_matrix = new Matrix();
    }

    System.Collections.IEnumerator QLearning_Coroutine()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        set_matrix();

        initialize_qtable();

        save_qTable();

        m_matrix.find_start_and_goal_positions(m_matrix.get_board());

        start_position = m_matrix.get_start();
        goal_position = m_matrix.get_goal();
        current_position = new int[2] { start_position[0], start_position[1] };

        DebugLog.Log("Start position: " + start_position[0] + ", " + start_position[1]);
        DebugLog.Log("Goal position: " + goal_position[0] + ", " + goal_position[1]);

        for (int e = 0; e < episodes; e++)
        {
            canvasManager.set_episode_text(e + 1, episodes);
            m_matrix.reset_cells_color();

            // Determina si se deben guardar los movimientos de este episodio
            bool save_this_episode = save_all_movements || (e % save_frequency == 0) || (e == episodes - 1);

            current_position = m_matrix.reset_to_new_random_position(current_position, e, 0);
            m_matrix.save_board(e, 0);

            int state = current_position[0] * m_columns + current_position[1];
            bool done = false;
            int steps = 0;
            int max_steps = m_rows * m_columns * 2;

            while (!done && steps < max_steps)
            {
                int action = epsilon_greedy_action(state, epsilon);

                float reward = take_action(m_matrix.get_board(), action, out int next_row, out int next_col, false, steps);

                int next_state = next_row * m_columns + next_col;

                // Encuentra el valor Q máximo en el siguiente estado
                float max_next_q = float.MinValue;
                for (int a = 0; a < 4; a++)
                {
                    max_next_q = Mathf.Max(max_next_q, qTable[next_state, a]);
                }

                // Actualiza el valor Q usando la fórmula de Q-Learning
                float current_q = qTable[state, action];
                
                if (reward != -Mathf.Infinity)
                {
                    qTable[state, action] = current_q + learning_rate * (reward + discount_factor * max_next_q - current_q);
                }

                // Verifica si se alcanzó el objetivo
                if (check_goal_reached(next_row, next_col))
                {
                    done = true;

                    int success_episode = e + 1;
                    int success_step = steps + 1;
                    success_episodes.Add(success_episode);
                    success_steps.Add(success_step);
                }

                // Actualiza el estado actual
                state = next_state;

                // Actualiza la posición en el tablero
                m_matrix.set_board_value(current_position[0], current_position[1], '0'); // Limpia la posición anterior
                current_position[0] = next_row;
                current_position[1] = next_col;
                m_matrix.set_board_value(current_position[0], current_position[1], 'X'); // Marca la nueva posición

                m_matrix.mark_cell_as_checked(current_position[0], current_position[1]);

                if (save_this_episode)
                {
                    m_matrix.save_board(e + 1, steps + 1);
                }

                steps++;
                canvasManager.set_step_text(steps + 1, max_steps);
                yield return new WaitForSeconds(stepDelay); // Pausa para visualización
            }

            // Reduce epsilon gradualmente al final de cada episodio
            epsilon = Mathf.Max(0.3f, epsilon * 0.99f); // Reduce epsilon pero no menos de 0.1
        }

        DebugLog.Log("Q-Learning training completed.");

        if (success_episodes.Count > 0)
        {
            DebugLog.Log("Successful paths found: " + success_episodes.Count);
        }
        else
        {
            DebugLog.Log("No successful paths found.");
        }

        // Guarda la Q-Table final
        save_qTable();

        stopwatch.Stop();

        DebugLog.Log("Q-Learning completed in " + stopwatch.Elapsed.TotalSeconds + " seconds.");

        // Demuestra el camino aprendido
        demonstrate_learned_path(m_matrix.get_board());
    }

    System.Collections.IEnumerator SARSA_Coroutine()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        set_matrix();

        initialize_qtable();

        save_qTable();

        m_matrix.find_start_and_goal_positions(m_matrix.get_board());

        start_position = m_matrix.get_start();
        goal_position  = m_matrix.get_goal();
        current_position = new int[2] { start_position[0], start_position[1] };

        DebugLog.Log("Start position: " + start_position[0] + ", " + start_position[1]);
        DebugLog.Log("Goal position: " + goal_position[0] + ", " + goal_position[1]);

        for (int e = 0; e < episodes; e++)
        {
            canvasManager.set_episode_text(e + 1, episodes);
            m_matrix.reset_cells_color();

            // Determine if we should save movements for this episode
            bool save_this_episode = save_all_movements || (e % save_frequency == 0) || (e == episodes - 1);

            current_position = m_matrix.reset_to_new_random_position(current_position, e, 0);
            m_matrix.save_board(e, 0);

            int state = current_position[0] * m_columns + current_position[1];
            bool done = false;
            int steps = 0;
            int max_steps = m_rows * m_columns * 2;

            int action = epsilon_greedy_action(state, epsilon);

            while (!done && steps < max_steps)
            {
                previous_position[0] = current_position[0];
                previous_position[1] = current_position[1];

                float reward = take_action(m_matrix.get_board(), action, out int next_row, out int next_col, false, steps);
                
                int next_state = next_row * m_columns + next_col;

                // Verificar si estamos en un ciclo infinito
                if (avoidLoops)
                {
                    if (detect_loop(state, next_state) == true)
                    {
                        // DebugLog.Log("Ciclo infinito detectado. Finalizando episodio.");
                        done = true;
                        break;
                    }
                }

                // Check if we reached the goal
                if (check_goal_reached(next_row, next_col))
                {
                    done = true;

                    int success_episode = e + 1;
                    int success_step = steps + 1;
                    success_episodes.Add(success_episode);
                    success_steps.Add(success_step);
                }

                int next_action = epsilon_greedy_action(next_state, epsilon);

                // Update Q-value using SARSA formula => //* Q(s, a) = Q(s, a) + α * (r + γ * Q(s', a') - Q(s, a))

                float current_q = qTable[state, action];
                float next_q = qTable[next_state, next_action];

                // Update Q-value
                if (reward != -Mathf.Infinity)
                {
                    qTable[state, action] = current_q + learning_rate * (reward + discount_factor * next_q - current_q);
                }

                // m_matrix.set_cell_qvalue(next_row, next_col, qTable[state, action], action); //! Esto sirve para visualizar el valor Q de las celdas, pero al activarlo no funciona bien el algoritmo

                // Update current state and action
                state = next_state;
                action = next_action;

                // Update position on the board

                m_matrix.set_board_value(current_position[0], current_position[1], '0'); // Clear previous position
                current_position[0] = next_row;
                current_position[1] = next_col;
                m_matrix.set_board_value(current_position[0], current_position[1], 'X'); // Set new position

                m_matrix.mark_cell_as_checked(current_position[0], current_position[1]);

                if (save_this_episode)
                {
                    m_matrix.save_board(e + 1, steps + 1);
                }

                steps++;
                canvasManager.set_step_text(steps + 1, max_steps);
                yield return new WaitForSeconds(stepDelay); // Delay for visualization
            }

            epsilon = Mathf.Max(0.3f, epsilon -0.001f); // Reduce epsilon pero no menos de 0.3
        }

        DebugLog.Log("SARSA training completed.");

        if (success_episodes.Count > 0)
        {
            DebugLog.Log("Successful paths found: " + success_episodes.Count);
            // print_successful_paths();
        }
        else
        {
            DebugLog.Log("No successful paths found.");
        }

        // Save the final Q-table
        save_qTable();

        stopwatch.Stop();
        DebugLog.Log("SARSA completed in " + stopwatch.Elapsed.TotalSeconds + " seconds.");

        // Demonstrate the learned path
        demonstrate_learned_path(m_matrix.get_board());
    }

    public void SARSA()
    {
        if (algorithmCoroutine != null)
        {
            StopCoroutine(algorithmCoroutine);
        }

        algorithmCoroutine = StartCoroutine(SARSA_Coroutine());
    }

    public void QLearning()
    {
        if (algorithmCoroutine != null)
        {
            StopCoroutine(algorithmCoroutine);
        }

        algorithmCoroutine = StartCoroutine(QLearning_Coroutine());
    }

    void StopAlgorithm()
    {
        if (algorithmCoroutine != null)
        {
            StopCoroutine(algorithmCoroutine);
            algorithmCoroutine = null;
        }
    }

    void print_successful_paths()
    {
        for (int i = 0; i < success_episodes.Count; i++)
        {
            DebugLog.Log($"Episode: {success_episodes[i]}, Steps: {success_steps[i]}");
        }
    }

    void set_matrix() //? Initialize the matrix and get board dimensions
    {
        m_rows = m_matrix.get_rows();
        m_columns = m_matrix.get_columns();
    }

    private int epsilon_greedy_action(int state, float epsilon)
    {
        // With probability epsilon, choose random action
        if (rand.NextDouble() < epsilon)
        {
            return rand.Next(4); // 0-3: Up, Down, Left, Right
        }

        // Otherwise, choose the action with the highest Q-value
        int best_action = 0;
        float best_value = float.MinValue;

        for (int a = 0; a < 4; a++)
        {
            if (qTable[state, a] > best_value)
            {
                best_value = qTable[state, a];
                best_action = a;
            }
        }

        return best_action;
    }

    private float take_action(char[,] board, int action, out int next_row, out int next_col, bool update_board = false, int step = 0)
    {
        // Default is to stay in current position
        next_row = current_position[0];
        next_col = current_position[1];

        bool hit_wall = false;
        bool found_gift = false;

        // Calculate potential next position based on action
        int potential_next_row = current_position[0];
        int potential_next_col = current_position[1];
        int previous_row = current_position[0];
        int previous_col = current_position[1];

        switch ((Action)action)
        {
            case Action.Up:
                if (current_position[0] > 0)
                {
                    potential_next_row = current_position[0] - 1;
                }
                break;
            case Action.Down:
                if (current_position[0] < m_rows - 1)
                {
                    potential_next_row = current_position[0] + 1;
                }
                break;
            case Action.Left:
                if (current_position[1] > 0)
                {
                    potential_next_col = current_position[1] - 1;
                }
                break;
            case Action.Right:
                if (current_position[1] < m_columns - 1)
                {
                    potential_next_col = current_position[1] + 1;
                }
                break;
        }

        // Validate indices
        if (potential_next_row < 0 || potential_next_row >= m_rows || potential_next_col < 0 || potential_next_col >= m_columns)
        {
            DebugLog.LogError($"Índices fuera de rango: ({potential_next_row}, {potential_next_col})");
            return -1; // Penalización por intentar moverse fuera de los límites
        }

        // Check what's in the potential next position
        if (potential_next_row != current_position[0] || potential_next_col != current_position[1])
        {
            char cell_content = board[potential_next_row, potential_next_col];

            if (cell_content == '1')
            {
                // Hit a wall - stay in current position
                hit_wall = true;
                // DebugLog.Log($"El agente chocó con una pared en ({current_position[0]}, {current_position[1]}) al intentar hacer la accion {action_to_string(action)}.");
            }
            else
            {
                next_row = potential_next_row;
                next_col = potential_next_col;
                // DebugLog.Log($"El agente se movió de ({current_position[0]}, {current_position[1]}) a ({potential_next_row}, {potential_next_col}) al hacer la accion {action_to_string(action)}.");
                if (cell_content == 'G')
                {
                    // Found a gift - move to that position
                    found_gift = true;

                    // If this is not just a simulation, change the gift to empty space
                    // so it can only be collected once
                    if (update_board)
                    {
                        m_matrix.set_board_value(next_row, next_col, '0');
                    }
                }
            }
        }

        // If requested, update the board visualization
        if (update_board)
        {
            m_matrix.set_board_value(current_position[0], current_position[1], '0'); // Clear previous position
            m_matrix.set_board_value(next_row, next_col, 'X'); // Set new position
        }

        // Calculate reward
        float reward;
        if (next_row == goal_position[0] && next_col == goal_position[1])
        {
            reward = goal_award; // Reached the goal
        }
        else if (hit_wall)
        {
            reward = -Mathf.Infinity; // Hit a wall
        }
        else if (found_gift)
        {
            reward = gift_award; // Collected a gift
        }
        else if (next_row == previous_position[0] && next_col == previous_position[1]) // If comes back to previous position
        {
            reward = -3;
        }
        else if (next_row == current_position[0] && next_col == current_position[1] && !hit_wall)
        {
            reward = -5; // Couldn't move (other reason)
            DebugLog.Log($"El agente no pudo moverse desde ({current_position[0]}, {current_position[1]}).");
        }
        else
        {
            reward = movement_award; // Standard move reward
        }
        // DebugLog.Log("Step " + (step + 1) + " - Action: " + action_to_string(action) + " - Reward: " + reward + " - Position: (" + next_row + ", " + next_col + ")" + "epsilon: " + epsilon);
        return reward;
    }

    private void save_qTable()
    {
        string data_folder = Path.Combine(Application.dataPath, "Data");
        string path = Path.Combine(data_folder, "QTable.txt");
        try
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine("Q-Table Values:");
                writer.WriteLine("Format: [State] [Action] [Q-Value]");
                writer.WriteLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                for (int s = 0; s < m_rows * m_columns; s++)
                {
                    int r = s / m_columns;
                    int c = s % m_columns;

                    if (m_matrix.get_board()[r, c] != '1')
                    {
                        for (int a = 0; a < 4; a++)
                        {
                            if (is_action_valid(a, r, c))
                            {
                                writer.WriteLine($"[{r},{c}] {(Action)a} {qTable[s, a]}");
                            }
                            else
                            {
                                writer.WriteLine($"[{r},{c}] {(Action)a} {-Mathf.Infinity}");
                            }
                        }
                    }
                }
            }
            DebugLog.Log("Q-Table saved to QTable.txt");
        }
        catch (Exception ex)
        {
            DebugLog.Log("Error saving Q-Table: " + ex.Message);
        }
    }

    bool is_action_valid(int action, int currentRow, int currentCol)
    {
        bool result = true;
        switch (action)
        {
            case 0:
                // DebugLog.Log("Action: Up");
                if (m_matrix.get_board()[currentRow - 1, currentCol] == '1')
                {
                    result = false; // Invalid action
                }
                break;
            case 1:
                // DebugLog.Log("Action: Down");
                if (m_matrix.get_board()[currentRow + 1, currentCol] == '1')
                {
                    result = false; // Invalid action
                }
                break;
            case 2:
                // DebugLog.Log("Action: Left");
                if (m_matrix.get_board()[currentRow, currentCol - 1] == '1')
                {
                    result = false; // Invalid action
                }
                break;
            case 3:
                // DebugLog.Log("Action: Right");
                if (m_matrix.get_board()[currentRow, currentCol + 1] == '1')
                {
                    result = false; // Invalid action
                }
                break;
            default:
                DebugLog.LogError("Invalid action");
                result = false;
                break;
        }

        return result;
        // DebugLog.Log($"Next position: ({nextRow}, {nextCol})");
    }

    private void demonstrate_learned_path(char[,] board)
    {
        clear_zeros_in_qtable();

        DebugLog.Log("Demonstrating learned path...");

        m_matrix.reset_cells_color();

        current_position = new int[2] { start_position[0], start_position[1] };

        m_matrix.reset_to_starting_cell(current_position, 1000, 0);

        m_matrix.mark_cell_as_visited(current_position[0], current_position[1]);

        int steps = 0;
        int max_steps = m_rows * m_columns * 2;
        bool reached_goal = false;

        while (steps < max_steps)
        {
            int state = current_position[0] * m_columns + current_position[1];

            // Choose the best action from current state
            int best_action = 0;
            float best_value = float.MinValue;

            for (int a = 0; a < 4; a++)
            {
                if (qTable[state, a] > best_value)
                {
                    best_value = qTable[state, a];
                    best_action = a;
                }
            }

            m_matrix.set_board_value(current_position[0], current_position[1], '0'); // Clear current position
            
            float action_reward = take_action(m_matrix.get_board(), best_action, out int next_row, out int next_col, true, steps);

            // DebugLog.Log("Moved from (" + current_position[0] + ", " + current_position[1] + ") to (" + next_row + ", " + next_col + ")");

            current_position = new int[] { next_row, next_col };

            m_matrix.mark_cell_as_visited(current_position[0], current_position[1]);

            // Check if reached goal
            if (current_position[0] == goal_position[0] && current_position[1] == goal_position[1])
            {
                DebugLog.Log("Goal reached!");
                reached_goal = true;
                break;
            }

            // Mark new position
            m_matrix.set_board_value(current_position[0], current_position[1], 'X');
            // m_matrix.print_board();
            m_matrix.save_board(1000, steps);

            steps++;
        }

        if (!reached_goal)
        {
            DebugLog.Log("Failed to reach the goal within the maximum number of steps.");
        }
    }

    int[] move_to_cell(int[] current_position, int action)
    {
        int[] new_position = new int[2] { current_position[0], current_position[1] };

        switch (action)
        {
            case 0: // Up
                new_position[0]--;
                break;
            case 1: // Down
                new_position[0]++;
                break;
            case 2: // Left
                new_position[1]--;
                break;
            case 3: // Right
                new_position[1]++;
                break;
        }

        return new_position;
    }

    string action_to_string(int action)
    {
        switch (action)
        {
            case 0:
                return "Up";
            case 1:
                return "Down";
            case 2:
                return "Left";
            case 3:
                return "Right";
            default:
                return "Invalid action";
        }
    }

    void clean_qTable()
    {
        for (int s = 0; s < m_rows * m_columns; s++)
        {
            for (int a = 0; a < 4; a++)
            {
                qTable[s, a] = 0f;
            }
        }
    }

    public void Reset()
    {
        clean_qTable();
        m_matrix.reset_cells_color();
        m_matrix.reset_to_starting_cell(current_position, 0, 0);
        canvasManager.set_step_text(0, 0);
        canvasManager.set_episode_text(0, 0);
        success_episodes.Clear();
        success_steps.Clear();
        StopAlgorithm();
        visited_states.Clear();
    }

    void initialize_qtable()
    {
        qTable = new float[m_rows * m_columns, 4]; // 4 actions: Up, Down, Left, Right
        for (int s = 0; s < m_rows * m_columns; s++)
        {
            int r = s / m_columns;
            int c = s % m_columns;

            if (m_matrix.get_board()[r, c] != '1') // Evita inicializar paredes
            {
                for (int a = 0; a < 4; a++)
                {
                    qTable[s, a] = 0f; // Inicializa Q-table con 0
                }
            }
            else
            {
                for (int a = 0; a < 4; a++)
                {
                    qTable[s, a] = -Mathf.Infinity;
                }
            }
        }
    }

    void clear_zeros_in_qtable()
    {
        for (int s = 0; s < m_rows * m_columns; s++)
        {
            for (int a = 0; a < 4; a++)
            {
                if (qTable[s, a] == 0.0f)
                {
                    qTable[s, a] = -Mathf.Infinity;
                }
            }
        }
    }

    bool detect_loop(int current_state, int next_state)
    {
        bool detected = false;

        if (visited_states.Count >= cycle_check_limit)
        {
            // Si el estado actual ya está en la lista de los últimos estados visitados
            if (visited_states[visited_states.Count - 2] == next_state && visited_states[visited_states.Count - 1] == current_state)
            {
                detected = true;
            }

            visited_states.RemoveAt(0);
        }

        visited_states.Add(current_state);
        return detected;
    }

    bool check_goal_reached(int row, int col)
    {
        if (row == goal_position[0] && col == goal_position[1])
        {
            return true;
        }
        return false;
    }
}
