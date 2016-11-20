#pragma once
#pragma once
#include "Matrix.h"

namespace ReedSolomon {

	class SquareMatrix : public Matrix {

	public:

		SquareMatrix(int rows);
		SquareMatrix(const SquareMatrix& m);

		virtual ~SquareMatrix();

		SquareMatrix& operator=(const SquareMatrix& m);

		void Invert();
	};
}