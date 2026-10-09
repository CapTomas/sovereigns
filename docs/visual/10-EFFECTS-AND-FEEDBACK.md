# 10 — Motion effects, environmental feedback and interaction

**Scope:** visual response to actual actions, movement and world state; not independent mechanics. **Game authority:** §§5, 20–24, 26. **Related:** weather, tactical, army and UI files.

## Hierarchy and budget

Effects must answer **what changed, where, and why**, not merely make the screen busy. Ranked: (1) unmistakable command feedback, (2) combat/environmental cause, (3) atmosphere. Any lower layer is removed or faded before it obscures the higher layers.

### Effects tied to game state

| Event | Intended visual response | Constraint |
|---|---|---|
| Regiment selected | Clear boundary, facing, roster linkage | Do not mask soldiers, terrain or nearby units |
| Move/formation order | Readable path/formation ghost, direction | Display planned intent, never pretend inaccessible path valid |
| Enemy discovered/lost | Appropriate contact marker, uncertainty | No silhouette when not known to faction |
| Archer volley | Small projectile groups/flight arcs at applicable detail | Flight behavior follows actual simulation including wind |
| Melee contact | Minor local disturbances, shield/weapon movement, gaps | No fantasy shockwaves or universally huge impact particles |
| Cavalry charge | Coordinated motion, local dust if soil is dry, momentum cue | No dust on flooded fields or exaggerated speed lines by default |
| Rout | Broken formation, chaotic direction, status/UI cue | Soldiers cannot vanish or teleport; routing remains a physical event |
| Rain/snow | Light atmosphere + world-surface response | Actual weather and particle density controls separate |
| Fire/smoke | Local flame and wind-driven smoke for simulated fire | Cosmetic effect may reduce density; hazard/visibility still truthful |
| Bridge destroyed | Structural state change, debris where supported | Must persist at coordinates and affect subsequent routes |
| Field harvested | Crop canopy/state shift, stubble or bare field | Follows field harvest state |

## Projectiles and contact

A projectile is primarily a tactical event with a subtle optional visual. At distance draw aggregated missile paths/volley sweeps but preserve trajectory patterns sufficiently to avoid showing impossible arcs or directions. When visible individually, arrows/bolts/javelins use their actual trajectory states. Cosmetic trails are low intensity and optional. Missed volleys and crosswind drift should be understandable, not magically random.

Melee should feel weighty through regimental contact, front-line pressure, spacing compression and controlled weapon/shield motion. Avoid graphic gore as a primary visual hook; use restrained casualty depictions and clear aftermath.

## Motion language

- State changes use short, predictable, interruptible transitions. Animations never delay an order or misrepresent when mechanics took effect.
- Movement of large formations carries inertia visually consistent with movement snapshots; camera interpolation doesn't invent extra progress.
- UI notification animation is purposeful, not continuous bounce, bloom or confetti.
- Environmental animation (vegetation sway, ripples, particles) may become cheaper at distant zoom but must remain coherent with major physical fields.
- Reduced-motion settings disable nonessential movement, flashes, shake and parallax (if any) while retaining state clarity.

## Audio adjacency

The separate sound-production work may use these same event identities—wind, rain, river, drums, cavalry, metal contact, morale, construction—but auditory effects cannot create authority or information that faction perception prohibits. No need to define final music instrumentation in this visual package.

## Acceptance

Test at representative dense 20,000-plus-soldier composition, with rain beginning mid-contact and a bridge destroyed. Remove all particles: gameplay must remain fully understandable. Add effects gradually until atmosphere improves, not until every free screen region is occupied. Capture frame-time changes and inspect selection readability in grayscale and reduced motion.
