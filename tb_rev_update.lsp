;; Title block revision update — per markup:
;;   delete old rows A (50% SD) and B (100% SD 02.07.26)
;;   rename C -> A, D -> B, move both rows down 2 rows to sit above the REV header
;;   REVISION NO -> B
;; Wrapped in one undo group: a single U reverts everything.
(vl-load-com)
(defun c:TBREV (/ d o ya yc dy h p)
  (setq d (vla-get-activedocument (vlax-get-acad-object)))
  (vla-startundomark d)
  (setq o (lambda (h) (vlax-ename->vla-object (handent h))))
  ;; row pitch = Y(old row A) - Y(old row C)  (2 rows)
  (setq ya (cadr (vlax-get (o "125E9") 'InsertionPoint))
        yc (cadr (vlax-get (o "125F3") 'InsertionPoint))
        dy (- ya yc))
  ;; delete old rows A and B
  (foreach h '("125E9" "125EA" "125EB" "125EC" "125ED"
               "125EE" "125EF" "125F0" "125F1" "125F2")
    (vla-delete (o h)))
  ;; rename revision letters
  (vla-put-textstring (o "125F3") "A")
  (vla-put-textstring (o "125F8") "B")
  ;; move remaining rows down into the freed rows
  (foreach h '("125F3" "125F4" "125F5" "125F6" "125F7"
               "125F8" "125F9" "125FA" "125FB" "125FC")
    (vla-move (o h) (vlax-3d-point 0 0 0) (vlax-3d-point 0 dy 0)))
  ;; revision number in title block
  (vla-put-textstring (o "124EA") "B")
  (vla-endundomark d)
  (vla-regen d acActiveViewport)
  (princ "\nTBREV_OK")
  (princ))
