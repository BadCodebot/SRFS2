#include "stdafx.h"
#include "SquareMatrix.h"
#include "GF16.h"
#include <iostream>
#include <cstdint>

namespace ReedSolomon {

	SquareMatrix::SquareMatrix(int rows) : Matrix(rows, rows) { }
	SquareMatrix::SquareMatrix(const SquareMatrix& m) : Matrix(m) {}

	SquareMatrix::~SquareMatrix() {}

	SquareMatrix& SquareMatrix::operator=(const SquareMatrix& m) {
		Matrix::operator=(m);
		return *this;
	}

	void SquareMatrix::Invert() {
		Matrix& m = (*this);

		SquareMatrix rv(GetRows());
		for (int i = 0; i < GetRows(); i++) rv[i][i] = 1;

		for (int r = 0; r < GetRows(); r++) {
			uint16_t inverse = GF16::Inverse(m[r][r]);
			m[r] *= inverse;
			rv[r] *= inverse;

			for (int r2 = 0; r2 < GetRows(); r2++) {
				if (r != r2) {
					uint16_t p = m[r2][r];
					m[r2] += p * m[r];
					rv[r2] += p * rv[r];
				}
			}
		}

		m = rv;
	}
}
