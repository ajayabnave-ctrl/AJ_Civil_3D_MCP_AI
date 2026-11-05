(defun c:TBDIAG (/ ss i e ed f)
  (setq f (open "C:/Civil3D_MCP/Civil3D-mcp/tb_batch/tb_diag.txt" "w"))
  (write-line (strcat "DWG=" (getvar "DWGNAME")) f)
  (if (setq ss (ssget "_X" '((0 . "MTEXT,TEXT") (67 . 1))))
    (progn
      (setq i 0)
      (repeat (sslength ss)
        (setq e (ssname ss i) ed (entget e) i (1+ i))
        (write-line (strcat (cdr (assoc 0 ed)) "|" (cdr (assoc 410 ed)) "|" (cdr (assoc 8 ed)) "|"
                            (rtos (cadr (assoc 10 ed)) 2 3) "," (rtos (caddr (assoc 10 ed)) 2 3) "|"
                            (itoa (cond ((cdr (assoc 71 ed))) (0))) "|"
                            (cdr (assoc 1 ed))) f))))
  (close f)
  (princ "\nTBDIAG_OK")
  (princ))
