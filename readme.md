# LevelScipt

LevelScript is an interpreted scripting langauge capable of running in a Unity game. LevelScript is intended to allow advanced interactivity in user created levels. The syntax is most similar to python, and it aims to be beginner friedly without holding back advanced users.
## Example
```python
def fire ():
  for angle in -30..30:
    fireball = spawn(projectiles.fireball)
    fireball.velocity = velocity(100, angle)
  end
end
```
An advanced rundown of the syntax is found here.
A detailed description of the interals found here.

The lexer or even parser can be modified or replaced to give your players your prefered syntax.
## Syntax

### Operators
```python
# Maths
+ Plus, - Subtract, - Negate, * Multiply, / Divide, ^ Power, % Modolus,
# Comparison
< Less_than , > Greater_than ,<= Less_than_or_equal_to, >= Greater_than_or_equal_to, == Equal_to
# Boolean
! Not, & And, | Or, !& Nor, !| Zor
# Other
. Member_access, [] Index, () Invoke, : Splice, .. Range
```
## Keywords
```python
var, def, if, elif, else, while, for
```
## Conversions
```python
int(), str(), float(), bool(), rotation()
```
