# GAM531 Final Project Team 3
---

The Shattering is a 2D Metroidvania game that allows players to explore, combat and gain powers through defeating enemies.

Created by: Marcos Ian Araujo Carneiro, Yuhan Zhao, Furqan Khurrum and Jackey Zhou

<p align="center">
  <img src="Screenshot.png" width="1000" alt="Centered cube render">
</p>

## How to run
```bash
# Clone the repo
git clone https://github.com/Yuhan-Zhao-Aiden/GAM531_Team3_Final.git
cd GAM531_Team3_Final/

# Run
dotnet run
```
Or open in visual studio and click run button

## How to play
- Press ```A/D``` to move left/right
- Press ```Space``` or ```W``` or ```Up Arrow``` 
- Press ```J``` to attack
- Press ```K``` to roll



## Animations


<table>
<tr>
<td>

**Player**
- Idle
- Running
- Jumping
- Falling
- Attack
- Roll
- Death

</td>
<td>

**Enemy**
- Walk
- Attack
- Death
- Idle

</td>
</tr>
</table>


## Feature
- Simple gravity
- Collision detection and resolver
- Projectile collision
- 2D animation renderer (SpriteRenderer class)
- Enemy AI (Ranged attack)
- Health system
- UI element (player and enemy health bar)

## Animation State Machine
- State Machine is embedded in Player class contains animation switcher
- Animations are loaded from corresponding sprite sheets in the OnLoad function. 
- Player.PlayAnimation function checks for current active animation, prevents the same animation from restarting if it's already playing.
- the Player class has a FacingDirection property in the base class, default to right, it changes depending if the player press A/D, and it causes player sprite UV to flip, so that the animation plays with the character facing the correct direction
- Animation always plays full loop to prevent animation from getting stuck at intermediate frame.

## Enemy AI Logic
- The enemy uses a distance-based state machine that tracks the player's position and adjusts behavior based on proximity
- Enemy behavior is calculated every frame in UpdateAI()
- if IsDead == true, enemy AI disabled (stop moving)
- Enemy maintain a set distance from the player, It will try to stay 400px from player
  - Move closer if player is too far
  - Move away if player is too close
- Animation is played accordingly

## Collision System
- The collision system uses Axis-Alligned Bounding Box (AABB)
- Handled by CollisionSystem class in Systems namespace
- Each game object has a collision rectangle that aligns with X and Y axis
- CheckAABBCollision() function taks 2 bounding box, and determine if they overlap

## Audio
- Audio system of The Shattering uses NAudio library and has 3 components
  - AudioSystem class main controller
  - Entities trigger events when action occurs
  - GameScene connects events with audio playback
- Audio of the game includes
  - BGM: Continuous loop of background music
  - SFX: sound effects triggered by events (shooting, slashing, explosion)
  - Footstep: randomized 3 types of footstep plays when player walks

## Challenges
- Cropping the correct part of the sprite was challenging, It was the reason my character disappear when i jump, because jumping animation sprites are incorrectly cropped, I used Microsoft Paint to get the exact coordinates of the sprites.
- All my Animation sprite sheets are facing one direction. i had to come up with a way to flip all the animation for the other direction. instead of manually flipping all the sprite sheets and load additional animation, I flipped UV coordinates of vertices instead. 
- Player sinking during attack: Fixed by using fixed collision height with render offest for taller sprites

## Credits
- Character sprite: https://aamatniekss.itch.io/fantasy-knight-free-pixelart-animated-character
- Enemy sprite: https://craftpix.net/freebies/free-satyr-sprite-sheet-pixel-art-pack/
- ground tile: https://www.shutterstock.com/image-vector/pixel-art-tile-set-2d-retro-2323393295
- Music: https://freetouse.com/music/pufino/enlivening
- Sound Effects:
  - Winning Effect by <a href="https://pixabay.com/users/superpuyofãns1234-45913026/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=404167">Sophia Conçeição</a> from <a href="https://pixabay.com/sound-effects//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=404167">Pixabay</a>
  - Footstep Effect by <a href="https://pixabay.com/users/joentnt-47713256/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=291984">Joen TNT</a> from <a href="https://pixabay.com/sound-effects//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=291984">Pixabay</a>
  - Shooting Effect by <a href="https://pixabay.com/users/freesound_community-46691455/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=102360">freesound_community</a> from <a href="https://pixabay.com/sound-effects//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=102360">Pixabay</a>
  - Sword Effect by <a href="https://pixabay.com/users/cyberwave-orchestra-23801316/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=339824">Cyberwave Orchestra</a> from <a href="https://pixabay.com/sound-effects//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=339824">Pixabay</a>
  - Game over Effect by <a href="https://pixabay.com/users/freesound_community-46691455/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=6435">freesound_community</a> from <a href="https://pixabay.com//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=6435">Pixabay</a>
  - Explosion Effect by <a href="https://pixabay.com/users/u_b32baquv5u-50250111/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=340454">u_b32baquv5u</a> from <a href="https://pixabay.com/sound-effects//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=340454">Pixabay</a>