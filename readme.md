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
