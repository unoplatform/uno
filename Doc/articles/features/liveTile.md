# Uno Support for Live Tiles

##Android##

  - only primary tile is supported,
  - `binding` is selected by current size of widget
    - e.g. for widget size 140..300, 'MediumTile' is selected
    - if there is no such binding, first smaller binding is used
    - if there is no smaller binding, first bigger binding is used
  - only `text` elements are supported, and only this attributes:
    - hint-style is converted to font size (so `body` and `base` are same)
    - hint-align
    - hint-wrap
    - hint-maxLines
    - hint-minLines
