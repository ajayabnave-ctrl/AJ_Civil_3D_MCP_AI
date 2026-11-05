(defun c:TBSHEETLIST (/ ss i e ed s n f)
  (setq n 0)
  (if (setq ss (ssget "_X" '((0 . "MTEXT,TEXT") (410 . "Model"))))
    (progn
      (setq i 0)
      (repeat (sslength ss)
        (setq e (ssname ss i) ed (entget e) s (cdr (assoc 1 ed)) i (1+ i))
        (if (wcmatch s "NC-CQT-###-####-RTC-DWG-LI-*-D")
          (progn
            (entmod (subst (cons 1 (strcat (substr s 1 (1- (strlen s))) "B")) (assoc 1 ed) ed))
            (setq n (1+ n)))))))
  (if (setq f (open *SL-LOG* "a"))
    (progn (write-line (strcat (getvar "DWGNAME") ",CHANGED=" (itoa n)) f) (close f)))
  (princ))
