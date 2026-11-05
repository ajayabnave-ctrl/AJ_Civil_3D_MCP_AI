(defun dm:strs (ed / s)
  (setq s "")
  (foreach g ed
    (if (and (member (car g) '(1 2 3 302 303)) (= (type (cdr g)) 'STR))
      (setq s (strcat s " [" (itoa (car g)) "]" (cdr g)))))
  s)
(defun c:TBDIAGMS (/ ss i e ed p f nm sub sed q)
  (setq f (open *DM-OUT* "w"))
  (write-line (strcat "DWG=" (getvar "DWGNAME")) f)
  (if (setq ss (ssget "_X" '((0 . "TEXT,MTEXT,ACAD_TABLE,INSERT") (410 . "Model"))))
    (progn
      (setq i 0)
      (repeat (sslength ss)
        (setq e (ssname ss i) ed (entget e) i (1+ i) p (cdr (assoc 10 ed)))
        (setq nm (cdr (assoc 0 ed)))
        (write-line (strcat nm "|" (cdr (assoc 8 ed)) "|" (rtos (car p) 2 2) "," (rtos (cadr p) 2 2) "|" (if (= nm "INSERT") (cdr (assoc 2 ed)) "") "|" (if (= nm "INSERT") "" (dm:strs ed))) f)
        (if (= nm "INSERT")
          (progn
            (setq sub (cdr (assoc -2 (entget (tblobjname "BLOCK" (cdr (assoc 2 ed)))))))
            (while sub
              (setq sed (entget sub))
              (if (member (cdr (assoc 0 sed)) '("TEXT" "MTEXT" "ACAD_TABLE"))
                (progn
                  (setq q (cdr (assoc 10 sed)))
                  (write-line (strcat "  DEF|" (cdr (assoc 0 sed)) "|" (cdr (assoc 8 sed)) "|" (rtos (car q) 2 2) "," (rtos (cadr q) 2 2) "|" (dm:strs sed)) f)))
              (setq sub (entnext sub))))))))
  (close f)
  (princ))
