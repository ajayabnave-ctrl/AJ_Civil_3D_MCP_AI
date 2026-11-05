(defun ir:base (p / i r)
  (setq i (strlen p))
  (while (and (> i 0) (not r))
    (if (member (substr p i 1) '("\\" "/")) (setq r (substr p (1+ i))))
    (setq i (1- i)))
  (if r r p))
(defun c:TBIMGRELINK (/ d ed p b hit nf nu f)
  (setq nf 0 nu 0)
  (if (setq d (dictsearch (namedobjdict) "ACAD_IMAGE_DICT"))
    (foreach g d
      (if (= (car g) 350)
        (progn
          (setq ed (entget (cdr g)) p (cdr (assoc 1 ed)) hit nil)
          (if (and p (> (strlen p) 0) (not (findfile p)))
            (progn
              (setq b (ir:base p))
              (foreach dir *IR-DIRS*
                (if (and (not hit) (findfile (strcat dir b))) (setq hit (strcat dir b))))
              (if hit
                (progn (entmod (subst (cons 1 hit) (assoc 1 ed) ed)) (setq nf (1+ nf)))
                (setq nu (1+ nu)))))))))
  (if (setq f (open *IR-LOG* "a"))
    (progn (write-line (strcat (getvar "DWGNAME") ",RELINKED=" (itoa nf) " STILL_MISSING=" (itoa nu)) f) (close f)))
  (princ))
