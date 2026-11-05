(setq *IMG-LOG* "C:/RevitAddinDev/CIVIL 3D_MCP Setup/tb_batch/tb_img_log.csv")
(defun c:TBIMG (/ d n ed p f)
  (setq n 0)
  (if (and *TB-SRCDIR* (setq d (dictsearch (namedobjdict) "ACAD_IMAGE_DICT")))
    (foreach g d
      (if (= (car g) 350)
        (progn
          (setq ed (entget (cdr g)) p (cdr (assoc 1 ed)))
          (if (and p (> (strlen p) 2)
                   (/= (substr p 2 1) ":")
                   (/= (substr p 1 2) "\\\\"))
            (progn
              (if (= (substr p 1 2) ".\\") (setq p (substr p 3)))
              (entmod (subst (cons 1 (strcat *TB-SRCDIR* p)) (assoc 1 ed) ed))
              (setq n (1+ n))))))))
  (if (setq f (open *IMG-LOG* "a"))
    (progn (write-line (strcat (getvar "DWGNAME") ",IMGPATHS_FIXED=" (itoa n)) f) (close f)))
  (princ))

