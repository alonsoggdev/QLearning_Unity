using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using UnityEngine;

public class AIController : MonoBehaviour
{

    private System.Random rand = new System.Random();

    Matrix      m_matrix;
    int         m_rows;
    int         m_columns;
    float[,]    qTable;

    int[] start_position;
    int[] goal_position;
    int[] current_position;

    GameManager.Algorithm algorithm;
    private int           learning_rate;
    private int           discount_factor;
    private int           goal_award;
    private int           movement_award;
    private int           gift_award;

    public void set_algorithm(int algorithm)
    {
        this.algorithm = (GameManager.Algorithm)algorithm;
    }

    public void set_learning_rate(int learning_rate)
    {
        this.learning_rate = learning_rate;
    }

    public void set_discount_factor(int discount_factor)
    {
        this.discount_factor = discount_factor;
    }

    public void set_goal_award(int goal_award)
    {
        this.goal_award = goal_award;
    }

    public void set_movement_award(int movement_award)
    {
        this.movement_award = movement_award;
    }

    public void set_gift_award(int gift_award)
    {
        this.gift_award = gift_award;
    }


    [Header("SARSA Parameters")]
    int episodes    = 1000; // Number of episodes
    float epsilon   = 0.6f; // Exploration rate

    // Control parameters for saving
    bool save_all_movements = false;    // Set to true if you want to save every movement (warning: creates large files)
    int save_frequency      = 100;      // Save movements for every Nth episode

    // Track when the first successful path is found
    int first_success_episode   = -1;
    int first_success_steps     = -1;

    enum Action
    {
        Up,
        Down,
        Left,
        Right
    }

    public void Start()
    {
        m_matrix = new Matrix();
    }

    public void SARSA()
    {
        set_matrix();

        qTable = new float[m_rows * m_columns, 4]; // 4 actions: Up, Down, Left, Right

        m_matrix.find_start_and_goal_positions(m_matrix.get_board());

        start_position = m_matrix.get_start();
        goal_position  = m_matrix.get_goal();
        current_position = new int[2] { start_position[0], start_position[1] };

        // Add save all movements logic? True by default with default values

        for (int e = 0; e < episodes; e++)
        {
            // Determine if we should save movements for this episode
            bool save_this_episode = save_all_movements || (e % save_frequency == 0) || (e == episodes - 1);

            m_matrix.reset_to_initial_position(current_position, e, 0);
            m_matrix.save_board(e, 0);

            int state = current_position[0] * m_columns + current_position[1];
            int action = epsilon_greedy_action(state, epsilon);

            bool done = false;
            int steps = 0;
            int max_steps = m_rows * m_columns * 2; // Arbitrary limit to prevent infinite loops

            while (!done && steps < max_steps)
            {
                float reward = take_action(m_matrix.get_board(), action, out int next_row, out int next_col);
                int next_state = next_row * m_columns + next_col;

                // Check if we reached the goal
                if (next_row == goal_position[0] && next_col == goal_position[1])
                {
                    done = true;
                    if (first_success_episode == -1)
                    {
                        first_success_episode = e + 1;
                        first_success_steps = steps + 1;
                    }
                }

                int next_action = epsilon_greedy_action(next_state, epsilon);

                // Update Q-value using SARSA formula => //* Q(s, a) = Q(s, a) + α * (r + γ * Q(s', a') - Q(s, a))

                float current_q = qTable[state, action];
                float next_q = qTable[next_state, next_action];

                // If it hits a wall, force Q-Value to be -1
                if (reward == -1 && next_row == current_position[0] && next_col == current_position[1])
                {
                    qTable[state, action] = -1;
                }
                else
                {
                    qTable[state, action] = current_q + learning_rate * (reward + discount_factor * next_q - current_q);
                }

                // Update current state and action
                state = next_state;
                action = next_action;

                // Update position on the board

                m_matrix.set_board_value(current_position[0], current_position[1], '0'); // Clear previous position
                current_position[0] = next_row;
                current_position[1] = next_col;
                m_matrix.set_board_value(current_position[0], current_position[1], 'X'); // Set new position

                if (save_this_episode)
                {
                    m_matrix.save_board(e + 1, steps + 1);
                }

                steps++;
            }

            // Display progress every 100 episodes
            if ((e + 1) % 100 == 0)
            {
                Debug.Log($"Episode {e + 1}/{episodes} completed. Steps: {steps}");
            }

        }

        Debug.Log("SARSA training completed.");

        if (first_success_episode != -1)
        {
            Debug.Log($"First successful path found in episode {first_success_episode} with {first_success_steps} steps.");
        }
        else
        {
            Debug.Log("No successful path found.");
        }

        // Save the final Q-table
        save_qTable();

        // Demonstrate the learned path
        demonstrate_learned_path(m_matrix.get_board());
    }

    public void QLearning()
    {
        
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

    private float take_action(char[,] board, int action, out int next_row, out int next_col, bool update_board = false)
    {
        // Default is to stay in current position
        next_row = current_position[0];
        next_col = current_position[1];
        bool hit_wall = false;
        bool found_gift = false;

        // Calculate potential next position based on action
        int potential_next_row = current_position[0];
        int potential_next_col = current_position[1];

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

        // Check what's in the potential next position
        if (potential_next_row != current_position[0] || potential_next_col != current_position[1])
        {
            char cell_content = board[potential_next_row, potential_next_col];

            if (cell_content == '1')
            {
                // Hit a wall - stay in current position
                hit_wall = true;
            }
            else if (cell_content == 'G')
            {
                // Found a gift - move to that position
                next_row = potential_next_row;
                next_col = potential_next_col;
                found_gift = true;

                // If this is not just a simulation, change the gift to empty space
                // so it can only be collected once
                if (update_board)
                {
                    m_matrix.set_board_value(next_row, next_col, '0');
                }
            }
            else
            {
                // Valid move to empty space or goal
                next_row = potential_next_row;
                next_col = potential_next_col;
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
        else if (found_gift)
        {
            reward = gift_award; // Collected a gift
        }
        else if (hit_wall)
        {
            reward = -1; // Hit a wall
        }
        else if (next_row == current_position[0] && next_col == current_position[1] && !hit_wall)
        {
            reward = -1; // Couldn't move (other reason)
        }
        else
        {
            reward = movement_award; // Standard move reward
        }

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

                for (int s = 0; s < m_rows * m_columns; s++)
                {
                    int r = s / m_columns;
                    int c = s % m_columns;

                    for (int a = 0; a < 4; a++)
                    {
                        writer.WriteLine($"[{r},{c}] {(Action)a} {qTable[s, a]:F4}");
                    }
                }
            }
            Debug.Log("Q-Table saved to QTable.txt");
        }
        catch (Exception ex)
        {
            Debug.Log("Error saving Q-Table: " + ex.Message);
        }
    }

    private void demonstrate_learned_path(char[,] board)
    {
        Debug.Log("Demonstrating learned path...");

        m_matrix.reset_to_initial_position(current_position, 1000, 0);

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

            // Take the best action
            m_matrix.set_board_value(current_position[0], current_position[1], '0'); // Clear current position
            float _ = take_action(m_matrix.get_board(), best_action, out int nextRow, out int nextCol);
            current_position[0] = nextRow;
            current_position[1] = nextCol;

            // Check if reached goal
            if (current_position[0] == goal_position[0] && current_position[1] == goal_position[1])
            {
                Debug.Log("Goal reached!");
                reached_goal = true;
                break;
            }

            // Mark new position
            m_matrix.set_board_value(current_position[0], current_position[1], 'X');
            m_matrix.save_board(1000, steps);

            // Sleep to visualize the movement //TODO


            steps++;
        }

        if (!reached_goal)
        {
            Debug.Log("Failed to reach the goal within the maximum number of steps.");
        }
    }

}
