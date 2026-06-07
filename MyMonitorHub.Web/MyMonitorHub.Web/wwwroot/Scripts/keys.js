var keys = {
    Backspace: 8,
    Tab: 9,
    Enter: 13,
    Shift: 16,
    Ctrl: 17,
    Alt: 18,
    Pause: 19,
    CapsLock: 20,
    Esc: 27,
    Spacebar: 32,
    PageUp: 33,
    PageDown: 34,
    End: 35,
    Home: 36,
    LeftArrow: 37,
    UpArrow: 38,
    RightArrow: 39,
    DownArrow: 40,
    Insert: 45,
    Delete: 46,
    Zero: 48,
    One: 49,
    Two: 50,
    Three: 51,
    Four: 52,
    Five: 53,
    Six: 54,
    Seven: 55,
    Eight: 56,
    Nine: 57,
    A: 65,
    B: 66,
    C: 67,
    D: 68,
    E: 69,
    F: 70,
    G: 71,
    H: 72,
    I: 73,
    J: 74,
    K: 75,
    L: 76,
    M: 77,
    N: 78,
    O: 79,
    P: 80,
    Q: 81,
    R: 82,
    S: 83,
    T: 84,
    U: 85,
    V: 86,
    W: 87,
    X: 88,
    Y: 89,
    Z: 90,
    LeftWindow: 91,
    RightWindow: 92,
    Select: 93,
    Num0: 96,
    Num1: 97,
    Num2: 98,
    Num3: 99,
    Num4: 100,
    Num5: 101,
    Num6: 102,
    Num7: 103,
    Num8: 104,
    Num9: 105,
    Multiply: 106,
    Add: 107,
    Subtract: 109,
    Decimal: 110,
    Divide: 111,
    F1: 112,
    F2: 113,
    F3: 114,
    F4: 115,
    F5: 116,
    F6: 117,
    F7: 118,
    F8: 119,
    F9: 120,
    F10: 121,
    F11: 122,
    F12: 123,
    NumLock: 144,
    ScrollLock: 145,
    SemiColon: 186,
    Equals: 187,
    Comma: 188,
    Dash: 189,
    Period: 190,
    ForwardSlash: 191,
    Grave: 192,
    OpenBracket: 219,
    BackSlash: 220,
    CloseBracket: 221,
    SingleQuote: 222,


    isInvisibleKey: function(keyCode) {

        var isInvisible =
        (keyCode === keys.Backspace) ||
        (keyCode === keys.Tab) ||
        (keyCode === keys.Enter) ||
        (keyCode === keys.Shift) ||
        (keyCode === keys.Ctrl) ||
        (keyCode === keys.Alt) ||
        (keyCode === keys.Esc) ||
        (keyCode === keys.End) ||
        (keyCode === keys.Home) ||
        (keyCode === keys.PageUp) ||
        (keyCode === keys.PageDown) ||
        (keyCode === keys.LeftArrow) ||
        (keyCode === keys.UpArrow) ||
        (keyCode === keys.RightArrow) ||
        (keyCode === keys.DownArrow) ||
        (keyCode === keys.Insert) ||
        (keyCode === keys.Delete) ||
        (keyCode === keys.F1) ||
        (keyCode === keys.F2) ||
        (keyCode === keys.F3) ||
        (keyCode === keys.F4) ||
        (keyCode === keys.F5) ||
        (keyCode === keys.F6) ||
        (keyCode === keys.F7) ||
        (keyCode === keys.F8) ||
        (keyCode === keys.F9) ||
        (keyCode === keys.F10) ||
        (keyCode === keys.F11) ||
        (keyCode === keys.F12);

        return isInvisible;
    }
}
