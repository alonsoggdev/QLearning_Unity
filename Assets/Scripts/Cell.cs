using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    public char Type { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public bool Up { get; set; }
    public bool Down { get; set; }
    public bool Left { get; set; }
    public bool Right { get; set; }
}
