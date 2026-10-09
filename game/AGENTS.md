# Godot client implementation scope

Godot renders, accepts commands and presents inspected/authorized state. Convert world coordinates (metres, +y north) to screen space only at the client boundary, and format units only for display (ADR-0004). It does not own alternate terrain, climate, armies, economic inventories, political authority or a second simulation clock. Rendering details may be generated on demand only when consistent with authoritative coordinates and durable changes. The visual identity document decides style; the gameplay specification decides mechanics.
