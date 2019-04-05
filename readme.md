LevelScipt

LevelScript is an interpreted scripting langauge capable of running in a Unity game. LevelScript is intended to allow advanced interactivity in user created levels. The syntax is most similar to python, and it aims to be beginner friedly without holding back advanced users.
Example
```python
def fire ():
  for angle in -30..30:
    fireball = spawn(projectiles.fireball)
    fire.velocity = velocity(100, angle)
  end
end
```
An advanced rundown of the syntax is found here.
A detailed description of the interals found here.

The lexer or even parser can be modified or replaced to give your players your prefered syntax.
Syntax

Operators Maths + Plus - Subtract / Negate * Multiply / Divide ^ Power % Modolus Comparison <Less than >Greater than <=Less than or equal to >= Greater than or equal to ==Equal to Boolean ! Not and & And or | Or nor !& Nor zor !| Zor Other . Member access [] Index Access () Invoke : Splice .. Range
Keywords

var Decare variable def Declare function if If statement elif Else if statment else Else statement while While statement for For statement
Conversions
```python
int() To int (float, string) str() To string (float, int, etc) float() To float (int, string) bool() To boolean (string) rotation() To quaternion (vector, xyz)
```
License

MIT License

Copyright (c) 2019 j-rah

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
